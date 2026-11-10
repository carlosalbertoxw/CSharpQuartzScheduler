using CSharpQuartzScheduler.Configuration;
using CSharpQuartzScheduler.Jobs;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.Options;
using Quartz;

namespace CSharpQuartzScheduler;

/// <summary>
/// Registro de las rutinas: servicios de negocio, opciones validadas y Quartz.
/// Vive fuera de <c>Program.cs</c> para que las pruebas puedan componer el mismo host.
/// </summary>
public static class RoutinesServiceCollectionExtensions
{
    public static IServiceCollection AddRoutines(this IServiceCollection services, IConfiguration configuration)
    {
        // Servicios de negocio: cada rutina de appsettings.json referencia uno por su clave ("Service").
        services.AddKeyedScoped<IRoutineService, ExampleRoutineService>(ExampleRoutineService.Key);
        services.AddKeyedScoped<IRoutineService, DailyReportService>(DailyReportService.Key);
        services.AddKeyedScoped<IRoutineService, ReportCleanupService>(ReportCleanupService.Key);

        // Dependencias de las rutinas de reportes.
        services.AddSingleton(TimeProvider.System);
        services
            .AddOptions<ReportsOptions>()
            .Bind(configuration.GetSection(ReportsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Opciones fuertemente tipadas y validadas al arrancar.
        var routinesSection = configuration.GetSection(RoutinesOptions.SectionName);
        services
            .AddOptions<RoutinesOptions>()
            .Configure(options => routinesSection.Bind(options.Jobs))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RoutinesOptions>, RoutinesOptionsValidator>();

        // Un job y un trigger por rutina habilitada.
        var routines = routinesSection.Get<List<RoutineOptions>>() ?? [];
        services.AddQuartz(q =>
        {
            for (var i = 0; i < routines.Count; i++)
            {
                if (!routines[i].Enabled)
                {
                    continue;
                }

                var index = i;
                var jobKey = new JobKey(routines[i].Name, routines[i].Group);

                q.AddJob<RoutineJob>(j => j
                    .WithIdentity(jobKey)
                    .UsingJobData(RoutineJob.ServiceKeyDataKey, routines[index].Service));

                // El trigger se construye al crear el scheduler, a partir de las opciones ya
                // validadas: un cron o zona horaria inválidos se reportan con un mensaje claro.
                q.AddTrigger((provider, t) =>
                {
                    var routine = provider.GetRequiredService<IOptions<RoutinesOptions>>().Value.Jobs[index];

                    t.ForJob(jobKey)
                     .WithIdentity($"{routine.Name}-trigger", routine.Group)
                     .WithCronSchedule(routine.CronExpression, cron => cron
                         .InTimeZone(routine.ResolveTimeZone())
                         // Si el servicio estuvo detenido, ignora los disparos perdidos y espera al siguiente.
                         .WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing));
                });
            }
        });

        return services;
    }
}

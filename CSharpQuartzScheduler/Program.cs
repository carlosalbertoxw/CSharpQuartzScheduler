using CSharpQuartzScheduler.Configuration;
using CSharpQuartzScheduler.Jobs;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;

// Un servicio de Windows arranca en C:\Windows\System32: fijamos la carpeta del ejecutable
// para que las rutas relativas (p. ej. logs/) queden junto al .exe.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

// Logger de arranque: registra fallos que ocurran antes de construir el host.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Iniciando CSharpQuartzScheduler...");

    var builder = Host.CreateApplicationBuilder(args);

    // Permite ejecutarse como servicio de Windows sin cambios de código.
    builder.Services.AddWindowsService(options => options.ServiceName = "CSharpQuartzScheduler");

    // Logging estructurado con Serilog, configurado desde appsettings.json.
    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Servicios de negocio: cada rutina de appsettings.json referencia uno por su clave ("Service").
    builder.Services.AddKeyedScoped<IRoutineService, RoutineService>(RoutineService.Key);
    builder.Services.AddKeyedScoped<IRoutineService, DailyReportService>(DailyReportService.Key);
    builder.Services.AddKeyedScoped<IRoutineService, ReportCleanupService>(ReportCleanupService.Key);

    // Dependencias de las rutinas de reportes.
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services
        .AddOptions<ReportsOptions>()
        .Bind(builder.Configuration.GetSection(ReportsOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    // Opciones fuertemente tipadas y validadas al arrancar.
    var routinesSection = builder.Configuration.GetSection(RoutinesOptions.SectionName);
    builder.Services
        .AddOptions<RoutinesOptions>()
        .Configure(options => routinesSection.Bind(options.Jobs))
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<RoutinesOptions>, RoutinesOptionsValidator>();

    // Registro de Quartz vía DI + hosted service: un job y un trigger por rutina habilitada.
    var routines = routinesSection.Get<List<RoutineOptions>>() ?? [];
    builder.Services.AddQuartz(q =>
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
            q.AddTrigger((services, t) =>
            {
                var routine = services.GetRequiredService<IOptions<RoutinesOptions>>().Value.Jobs[index];

                t.ForJob(jobKey)
                 .WithIdentity($"{routine.Name}-trigger", routine.Group)
                 .WithCronSchedule(routine.CronExpression, cron => cron
                     .InTimeZone(routine.ResolveTimeZone())
                     // Si el servicio estuvo detenido, ignora los disparos perdidos y espera al siguiente.
                     .WithMisfireInstruction(CronTriggerMisfireInstruction.DoNothing));
            });
        }
    });

    // Espera a que los jobs en curso terminen antes de apagar el proceso.
    builder.Services.AddQuartzHostedService(options =>
    {
        options.WaitForJobsToComplete = true;
    });

    var host = builder.Build();
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó de forma inesperada.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

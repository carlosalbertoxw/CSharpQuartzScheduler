using CSharpQuartzScheduler.Configuration;
using CSharpQuartzScheduler.Jobs;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;

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

    // Opciones fuertemente tipadas y validadas al arrancar.
    builder.Services
        .AddOptions<RoutineJobOptions>()
        .Bind(builder.Configuration.GetSection(RoutineJobOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddScoped<IRoutineService, RoutineService>();

    // Registro de Quartz vía DI + hosted service.
    builder.Services.AddQuartz(q =>
    {
        var options = builder.Configuration
            .GetSection(RoutineJobOptions.SectionName)
            .Get<RoutineJobOptions>() ?? new RoutineJobOptions();

        if (options.Enabled)
        {
            var jobKey = new JobKey(options.Name, options.Group);

            q.AddJob<RoutineJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(t =>
            {
                t.ForJob(jobKey)
                 .WithIdentity($"{options.Name}-trigger", options.Group)
                 .WithCronSchedule(options.CronExpression, cron =>
                 {
                     if (!string.IsNullOrWhiteSpace(options.TimeZone))
                     {
                         cron.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone));
                     }
                 });
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

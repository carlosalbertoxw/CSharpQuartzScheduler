using CSharpQuartzScheduler;
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
    builder.Services.AddSerilog(cfg => cfg.ReadFrom.Configuration(builder.Configuration));

    // Servicios de negocio, opciones validadas y un job + trigger por rutina habilitada.
    builder.Services.AddRoutines(builder.Configuration);

    // Espera a que los jobs en curso terminen antes de apagar el proceso.
    builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

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

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Rutina mínima de ejemplo: solo registra inicio y fin. Úsala como plantilla para
/// lógica real (llamadas a API, consultas a BD, etc.); para un ejemplo con configuración
/// y dependencias propias, ver <see cref="DailyReportService"/>.
/// </summary>
public sealed class ExampleRoutineService(ILogger<ExampleRoutineService> logger) : IRoutineService
{
    /// <summary>Clave con la que se registra el servicio y se referencia en "Routines:Service".</summary>
    public const string Key = "Example";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Rutina iniciada.");

        // Simula trabajo asíncrono respetando la cancelación (apagado ordenado).
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

        logger.LogInformation("Rutina finalizada correctamente.");
    }
}

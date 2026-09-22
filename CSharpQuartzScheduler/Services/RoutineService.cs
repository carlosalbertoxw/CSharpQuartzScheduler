using Microsoft.Extensions.Logging;

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Rutina mínima de ejemplo: solo registra inicio y fin. Úsala como plantilla para
/// lógica real (llamadas a API, consultas a BD, etc.); para un ejemplo con configuración
/// y dependencias propias, ver <see cref="DailyReportService"/>.
/// </summary>
public sealed class RoutineService : IRoutineService
{
    /// <summary>Clave con la que se registra el servicio y se referencia en "Routines:Service".</summary>
    public const string Key = "Example";

    private readonly ILogger<RoutineService> _logger;

    public RoutineService(ILogger<RoutineService> logger) => _logger = logger;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rutina iniciada a las {Timestamp:O}", DateTimeOffset.Now);

        // Simula trabajo asíncrono respetando la cancelación (apagado ordenado).
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

        _logger.LogInformation("Rutina finalizada correctamente.");
    }
}

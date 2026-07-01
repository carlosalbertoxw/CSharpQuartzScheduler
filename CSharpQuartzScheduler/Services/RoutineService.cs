using Microsoft.Extensions.Logging;

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Implementación de ejemplo de la rutina. Aquí va la lógica real de negocio
/// (llamadas a API, consultas a BD, generación de reportes, etc.).
/// </summary>
public sealed class RoutineService : IRoutineService
{
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

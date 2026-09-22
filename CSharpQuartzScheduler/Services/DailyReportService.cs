using System.Text;
using CSharpQuartzScheduler.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Rutina de ejemplo que genera un reporte de texto con un resumen del sistema.
/// Muestra cómo una rutina recibe su propia configuración (<see cref="ReportsOptions"/>)
/// y dependencias (<see cref="TimeProvider"/>) por inyección.
/// </summary>
public sealed class DailyReportService : IRoutineService
{
    /// <summary>Clave con la que se registra el servicio y se referencia en "Routines:Service".</summary>
    public const string Key = "DailyReport";

    /// <summary>Prefijo de los archivos de reporte; la limpieza solo borra archivos con este patrón.</summary>
    public const string FilePrefix = "report-";

    private readonly ReportsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DailyReportService> _logger;

    public DailyReportService(IOptions<ReportsOptions> options, TimeProvider timeProvider, ILogger<DailyReportService> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var directory = Directory.CreateDirectory(Path.GetFullPath(_options.Directory));
        var path = Path.Combine(directory.FullName, $"{FilePrefix}{now:yyyyMMdd-HHmmss}.txt");

        var content = new StringBuilder()
            .AppendLine($"Reporte generado: {now:O}")
            .AppendLine($"Equipo:           {Environment.MachineName}")
            .AppendLine($"Sistema:          {Environment.OSVersion}")
            .AppendLine($"Procesadores:     {Environment.ProcessorCount}")
            .AppendLine($"Memoria en uso:   {GC.GetTotalMemory(forceFullCollection: false) / 1024} KB")
            .ToString();

        await File.WriteAllTextAsync(path, content, cancellationToken);

        _logger.LogInformation("Reporte diario generado en {ReportPath}", path);
    }
}


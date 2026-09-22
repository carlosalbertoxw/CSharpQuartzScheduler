using CSharpQuartzScheduler.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Rutina de ejemplo que borra los reportes de <see cref="DailyReportService"/> más antiguos
/// que <see cref="ReportsOptions.RetentionDays"/>. Solo toca archivos con el prefijo de reporte.
/// </summary>
public sealed class ReportCleanupService : IRoutineService
{
    /// <summary>Clave con la que se registra el servicio y se referencia en "Routines:Service".</summary>
    public const string Key = "ReportCleanup";

    private readonly ReportsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportCleanupService> _logger;

    public ReportCleanupService(IOptions<ReportsOptions> options, TimeProvider timeProvider, ILogger<ReportCleanupService> logger)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(_options.Directory));
        if (!directory.Exists)
        {
            _logger.LogInformation("No existe la carpeta de reportes {ReportDirectory}; nada que limpiar.", directory.FullName);
            return Task.CompletedTask;
        }

        var cutoff = _timeProvider.GetUtcNow().AddDays(-_options.RetentionDays);
        var deleted = 0;

        foreach (var file in directory.EnumerateFiles($"{DailyReportService.FilePrefix}*.txt"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.LastWriteTimeUtc < cutoff)
            {
                file.Delete();
                deleted++;
            }
        }

        _logger.LogInformation(
            "Limpieza de reportes: {DeletedCount} archivo(s) con más de {RetentionDays} días eliminados.",
            deleted, _options.RetentionDays);

        return Task.CompletedTask;
    }
}

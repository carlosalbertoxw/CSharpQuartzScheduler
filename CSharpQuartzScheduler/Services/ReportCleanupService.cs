using CSharpQuartzScheduler.Configuration;
using Microsoft.Extensions.Options;

namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Rutina de ejemplo que borra los reportes de <see cref="DailyReportService"/> más antiguos
/// que <see cref="ReportsOptions.RetentionDays"/>. Solo toca archivos con el prefijo de reporte.
/// </summary>
public sealed class ReportCleanupService(
    IOptions<ReportsOptions> options,
    TimeProvider timeProvider,
    ILogger<ReportCleanupService> logger) : IRoutineService
{
    /// <summary>Clave con la que se registra el servicio y se referencia en "Routines:Service".</summary>
    public const string Key = "ReportCleanup";

    public Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var directory = new DirectoryInfo(Path.GetFullPath(settings.Directory));
        if (!directory.Exists)
        {
            logger.LogInformation("No existe la carpeta de reportes {ReportDirectory}; nada que limpiar.", directory.FullName);
            return Task.CompletedTask;
        }

        var cutoff = timeProvider.GetUtcNow().AddDays(-settings.RetentionDays);
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

        logger.LogInformation(
            "Limpieza de reportes: {DeletedCount} archivo(s) con más de {RetentionDays} días eliminados.",
            deleted, settings.RetentionDays);

        return Task.CompletedTask;
    }
}

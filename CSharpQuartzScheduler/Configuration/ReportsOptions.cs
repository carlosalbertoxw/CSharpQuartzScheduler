using System.ComponentModel.DataAnnotations;

namespace CSharpQuartzScheduler.Configuration;

/// <summary>
/// Configuración compartida por las rutinas de reportes (sección "Reports" de appsettings.json).
/// </summary>
public sealed class ReportsOptions
{
    public const string SectionName = "Reports";

    /// <summary>Carpeta donde se escriben los reportes. Relativa = junto al ejecutable.</summary>
    [Required]
    public string Directory { get; set; } = "reports";

    /// <summary>Días que se conservan los reportes antes de que la limpieza los borre.</summary>
    [Range(1, 3650)]
    public int RetentionDays { get; set; } = 7;
}

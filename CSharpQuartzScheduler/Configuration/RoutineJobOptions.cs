using System.ComponentModel.DataAnnotations;

namespace CSharpQuartzScheduler.Configuration;

/// <summary>
/// Configuración de la rutina programada, enlazada desde la sección "RoutineJob" de appsettings.json.
/// </summary>
public sealed class RoutineJobOptions
{
    public const string SectionName = "RoutineJob";

    /// <summary>Nombre lógico del job (usado en la identidad de Quartz).</summary>
    [Required]
    public string Name { get; set; } = "RoutineJob";

    /// <summary>Grupo al que pertenece el job/trigger en Quartz.</summary>
    [Required]
    public string Group { get; set; } = "Routines";

    /// <summary>
    /// Expresión cron de Quartz (7 campos: seg min hora díaMes mes díaSemana [año]).
    /// Ejemplo: "0 0/1 * 1/1 * ? *" = cada minuto.
    /// </summary>
    [Required]
    public string CronExpression { get; set; } = "0 0/1 * 1/1 * ? *";

    /// <summary>Zona horaria IANA/Windows para evaluar el cron. Vacío = UTC.</summary>
    public string? TimeZone { get; set; }

    /// <summary>Si está deshabilitado, el job no se registra al arrancar.</summary>
    public bool Enabled { get; set; } = true;
}

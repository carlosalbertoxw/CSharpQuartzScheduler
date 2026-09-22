namespace CSharpQuartzScheduler.Configuration;

/// <summary>
/// Lista de rutinas programadas, enlazada desde el arreglo "Routines" de appsettings.json.
/// </summary>
public sealed class RoutinesOptions
{
    public const string SectionName = "Routines";

    public List<RoutineOptions> Jobs { get; } = [];
}

/// <summary>
/// Configuración de una rutina: qué servicio ejecutar y cuándo.
/// </summary>
public sealed class RoutineOptions
{
    /// <summary>Nombre lógico del job (usado en la identidad de Quartz).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Grupo al que pertenece el job/trigger en Quartz.</summary>
    public string Group { get; set; } = "Routines";

    /// <summary>
    /// Clave del <see cref="Services.IRoutineService"/> registrado con
    /// <c>AddKeyedScoped</c> que ejecuta esta rutina.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Expresión cron de Quartz (6-7 campos: seg min hora díaMes mes díaSemana [año]).
    /// Ejemplo: "0 * * ? * *" = cada minuto.
    /// </summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>Zona horaria (ID de Windows o IANA) para evaluar el cron. Vacío = UTC.</summary>
    public string? TimeZone { get; set; }

    /// <summary>Si está deshabilitada, la rutina no se programa al arrancar.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Zona horaria efectiva: la configurada o UTC si está vacía.</summary>
    public TimeZoneInfo ResolveTimeZone() =>
        string.IsNullOrWhiteSpace(TimeZone) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
}

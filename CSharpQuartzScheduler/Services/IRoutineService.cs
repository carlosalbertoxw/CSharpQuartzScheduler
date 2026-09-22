namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Contrato del trabajo de negocio que ejecuta una rutina programada.
/// Cada implementación se registra con <c>AddKeyedScoped</c> y una clave, que las
/// entradas de "Routines" en appsettings.json referencian en su campo "Service".
/// Separar la lógica del <see cref="Quartz.IJob"/> permite testearla de forma aislada.
/// </summary>
public interface IRoutineService
{
    /// <summary>
    /// Ejecuta la rutina. Se crea un scope de DI nuevo por ejecución.
    /// </summary>
    /// <param name="cancellationToken">Se cancela al apagar el host; respétalo para un apagado ordenado.</param>
    Task RunAsync(CancellationToken cancellationToken);
}

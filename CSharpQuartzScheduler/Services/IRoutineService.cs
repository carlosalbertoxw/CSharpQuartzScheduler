namespace CSharpQuartzScheduler.Services;

/// <summary>
/// Contrato del trabajo de negocio que ejecuta la rutina programada.
/// Separar la lógica del <see cref="Quartz.IJob"/> permite testearla de forma aislada.
/// </summary>
public interface IRoutineService
{
    Task RunAsync(CancellationToken cancellationToken);
}

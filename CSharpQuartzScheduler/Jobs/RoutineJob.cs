using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CSharpQuartzScheduler.Jobs;

/// <summary>
/// Job de Quartz que delega el trabajo real en <see cref="IRoutineService"/>.
/// <para>
/// - <see cref="DisallowConcurrentExecutionAttribute"/> evita que una ejecución lenta
///   se solape con la siguiente.
/// - Las excepciones se re-lanzan como <see cref="JobExecutionException"/> para que
///   Quartz aplique su política de reintento/registro.
/// </para>
/// </summary>
[DisallowConcurrentExecution]
public sealed class RoutineJob : IJob
{
    private readonly IRoutineService _routineService;
    private readonly ILogger<RoutineJob> _logger;

    public RoutineJob(IRoutineService routineService, ILogger<RoutineJob> logger)
    {
        _routineService = routineService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _routineService.RunAsync(context.CancellationToken);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // Apagado ordenado: no es un error, no reintentar.
            _logger.LogWarning("Job {JobKey} cancelado por apagado del host.", context.JobDetail.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el job {JobKey}.", context.JobDetail.Key);
            // refireImmediately: false -> deja que el siguiente disparo del cron lo reintente.
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}

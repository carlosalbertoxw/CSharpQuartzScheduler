using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CSharpQuartzScheduler.Jobs;

/// <summary>
/// Job de Quartz genérico: resuelve el <see cref="IRoutineService"/> indicado en su
/// JobDataMap y le delega el trabajo real.
/// <para>
/// - <see cref="DisallowConcurrentExecutionAttribute"/> evita que una ejecución lenta
///   se solape con la siguiente (por job, no entre rutinas distintas).
/// - Las excepciones se re-lanzan como <see cref="JobExecutionException"/> para que
///   Quartz aplique su política de reintento/registro.
/// </para>
/// </summary>
[DisallowConcurrentExecution]
public sealed class RoutineJob : IJob
{
    /// <summary>Clave del JobDataMap con la clave del servicio a ejecutar.</summary>
    public const string ServiceKeyDataKey = "RoutineService";

    // Proveedor del scope que Quartz crea para cada ejecución.
    private readonly IServiceProvider _services;
    private readonly ILogger<RoutineJob> _logger;

    public RoutineJob(IServiceProvider services, ILogger<RoutineJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            if (!context.MergedJobDataMap.TryGetValue(ServiceKeyDataKey, out var serviceKey) || serviceKey is not string key)
            {
                throw new InvalidOperationException($"El job {context.JobDetail.Key} no define '{ServiceKeyDataKey}'.");
            }

            var routineService = _services.GetRequiredKeyedService<IRoutineService>(key);
            await routineService.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Apagado ordenado: no es un error, no reintentar.
            _logger.LogWarning("Job {JobKey} cancelado por apagado del host.", context.JobDetail.Key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falló el job {JobKey}.", context.JobDetail.Key);
            // RefireImmediately queda en false: el siguiente disparo del cron lo reintenta.
            throw new JobExecutionException(ex);
        }
    }
}

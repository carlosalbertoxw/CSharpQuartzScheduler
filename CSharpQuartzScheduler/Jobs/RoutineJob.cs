using CSharpQuartzScheduler.Services;
using Quartz;

namespace CSharpQuartzScheduler.Jobs;

/// <summary>
/// Job de Quartz genérico: resuelve el <see cref="IRoutineService"/> indicado en su
/// JobDataMap y le delega el trabajo real.
/// <para>
/// - <see cref="DisallowConcurrentExecutionAttribute"/> evita que una ejecución lenta
///   se solape con la siguiente (por job, no entre rutinas distintas).
/// - Las excepciones se re-lanzan como <see cref="JobExecutionException"/>; Quartz las
///   registra una sola vez y aplica su política de reintento.
/// - Cada línea de log escrita durante la ejecución lleva la propiedad <c>Routine</c>.
/// </para>
/// </summary>
/// <param name="services">Proveedor del scope que Quartz crea para cada ejecución.</param>
/// <param name="logger">Logger del job.</param>
[DisallowConcurrentExecution]
public sealed class RoutineJob(IServiceProvider services, ILogger<RoutineJob> logger) : IJob
{
    /// <summary>Clave del JobDataMap con la clave del servicio a ejecutar.</summary>
    public const string ServiceKeyDataKey = "RoutineService";

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobKey = context.JobDetail.Key;
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["Routine"] = jobKey.ToString() });

        try
        {
            if (!context.MergedJobDataMap.TryGetValue(ServiceKeyDataKey, out var serviceKey) || serviceKey is not string key)
            {
                throw new InvalidOperationException($"El job {jobKey} no define '{ServiceKeyDataKey}'.");
            }

            var routineService = services.GetRequiredKeyedService<IRoutineService>(key);
            await routineService.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Apagado ordenado: no es un error, no reintentar.
            logger.LogWarning("Job cancelado por apagado del host.");
        }
        catch (Exception ex)
        {
            // Quartz registra el error (JobRunShell, nivel Error) con la clave del job: aquí no
            // se registra para no duplicarlo. RefireImmediately queda en false: el siguiente
            // disparo del cron lo reintenta.
            throw new JobExecutionException(ex);
        }
    }
}

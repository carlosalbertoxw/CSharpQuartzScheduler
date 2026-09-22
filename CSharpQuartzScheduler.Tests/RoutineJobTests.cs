using CSharpQuartzScheduler.Jobs;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Quartz;

namespace CSharpQuartzScheduler.Tests;

public class RoutineJobTests
{
    private const string ServiceKey = "Test";

    private readonly IRoutineService _routineService = Substitute.For<IRoutineService>();

    private RoutineJob CreateJob()
    {
        var services = new ServiceCollection()
            .AddKeyedSingleton(ServiceKey, _routineService)
            .BuildServiceProvider();

        return new RoutineJob(services, NullLogger<RoutineJob>.Instance);
    }

    private static IJobExecutionContext CreateContext(string? serviceKey = ServiceKey)
    {
        var dataMap = new JobDataMap();
        if (serviceKey is not null)
        {
            dataMap.Add(RoutineJob.ServiceKeyDataKey, serviceKey);
        }

        var jobDetail = Substitute.For<IJobDetail>();
        jobDetail.Key.Returns(new JobKey("Job", "Tests"));

        var context = Substitute.For<IJobExecutionContext>();
        context.MergedJobDataMap.Returns(dataMap);
        context.JobDetail.Returns(jobDetail);
        return context;
    }

    [Fact]
    public async Task Execute_RunsConfiguredService()
    {
        using var cts = new CancellationTokenSource();

        await CreateJob().Execute(CreateContext(), cts.Token);

        await _routineService.Received(1).RunAsync(cts.Token);
    }

    [Fact]
    public async Task Execute_WrapsFailuresInJobExecutionException()
    {
        var failure = new InvalidOperationException("boom");
        _routineService.RunAsync(Arg.Any<CancellationToken>()).ThrowsAsync(failure);

        var ex = await Assert.ThrowsAsync<JobExecutionException>(
            async () => await CreateJob().Execute(CreateContext(), CancellationToken.None));

        Assert.Same(failure, ex.InnerException);
        Assert.False(ex.RefireImmediately);
    }

    [Fact]
    public async Task Execute_SwallowsCancellationDuringShutdown()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _routineService.RunAsync(cts.Token).ThrowsAsync(new OperationCanceledException(cts.Token));

        await CreateJob().Execute(CreateContext(), cts.Token);
    }

    [Fact]
    public async Task Execute_FailsWhenServiceKeyIsMissing()
    {
        var ex = await Assert.ThrowsAsync<JobExecutionException>(
            async () => await CreateJob().Execute(CreateContext(serviceKey: null), CancellationToken.None));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }
}

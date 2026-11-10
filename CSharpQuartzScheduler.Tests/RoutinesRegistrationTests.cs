using CSharpQuartzScheduler.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;

namespace CSharpQuartzScheduler.Tests;

/// <summary>
/// Compone el host con el appsettings.json real, igual que Program.cs: detecta errores de
/// configuración o servicios sin registrar sin tener que ejecutar la aplicación.
/// </summary>
public class RoutinesRegistrationTests
{
    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory,
            EnvironmentName = Environments.Production,
        });
        builder.Services.AddRoutines(builder.Configuration);
        return builder.Build();
    }

    [Fact]
    public void BundledConfiguration_IsValid()
    {
        using var host = BuildHost();

        // Resolver las opciones ejecuta los validadores (lanza OptionsValidationException si fallan).
        var routines = host.Services.GetRequiredService<IOptions<RoutinesOptions>>().Value;
        var reports = host.Services.GetRequiredService<IOptions<ReportsOptions>>().Value;

        Assert.Equal(["Example", "DailyReport", "ReportCleanup"], routines.Jobs.Select(r => r.Name));
        Assert.Equal("reports", reports.Directory);
    }

    [Fact]
    public async Task BundledConfiguration_SchedulesOneTriggerPerEnabledRoutine()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var host = BuildHost();

        var scheduler = await host.Services.GetRequiredService<ISchedulerFactory>().GetScheduler(cancellationToken);
        try
        {
            var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup(), cancellationToken);

            Assert.Equal(3, triggerKeys.Count);
            Assert.Contains(new TriggerKey("DailyReport-trigger", "Reports"), triggerKeys);
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: false, cancellationToken);
        }
    }
}

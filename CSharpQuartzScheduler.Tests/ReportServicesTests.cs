using CSharpQuartzScheduler.Configuration;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CSharpQuartzScheduler.Tests;

public sealed class ReportServicesTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 7, 0, 0, TimeSpan.Zero);

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"reports-{Guid.NewGuid():N}");
    private readonly IOptions<ReportsOptions> _options;
    private readonly FixedTimeProvider _timeProvider = new(Now);

    public ReportServicesTests()
    {
        _options = Options.Create(new ReportsOptions { Directory = _directory, RetentionDays = 7 });
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public async Task DailyReport_WritesReportFile()
    {
        var service = new DailyReportService(_options, _timeProvider, NullLogger<DailyReportService>.Instance);

        await service.RunAsync(CancellationToken.None);

        var file = Path.Combine(_directory, "report-20260922-070000.txt");
        Assert.True(File.Exists(file));
        Assert.Contains("Reporte generado: 2026-09-22T07:00:00", await File.ReadAllTextAsync(file));
    }

    [Fact]
    public async Task Cleanup_DeletesOnlyExpiredReports()
    {
        Directory.CreateDirectory(_directory);
        var expired = CreateFile("report-old.txt", Now.AddDays(-8));
        var recent = CreateFile("report-new.txt", Now.AddDays(-6));
        var unrelated = CreateFile("otro-archivo.txt", Now.AddDays(-30));

        var service = new ReportCleanupService(_options, _timeProvider, NullLogger<ReportCleanupService>.Instance);
        await service.RunAsync(CancellationToken.None);

        Assert.False(File.Exists(expired));
        Assert.True(File.Exists(recent));
        Assert.True(File.Exists(unrelated));
    }

    [Fact]
    public async Task Cleanup_MissingDirectory_DoesNothing()
    {
        var service = new ReportCleanupService(_options, _timeProvider, NullLogger<ReportCleanupService>.Instance);

        await service.RunAsync(CancellationToken.None);

        Assert.False(Directory.Exists(_directory));
    }

    private string CreateFile(string name, DateTimeOffset lastWrite)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "x");
        File.SetLastWriteTimeUtc(path, lastWrite.UtcDateTime);
        return path;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

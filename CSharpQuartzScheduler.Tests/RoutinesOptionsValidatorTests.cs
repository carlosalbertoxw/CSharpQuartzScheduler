using CSharpQuartzScheduler.Configuration;
using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace CSharpQuartzScheduler.Tests;

public class RoutinesOptionsValidatorTests
{
    private readonly RoutinesOptionsValidator _validator;

    public RoutinesOptionsValidatorTests()
    {
        var services = new ServiceCollection()
            .AddKeyedSingleton("Example", Substitute.For<IRoutineService>())
            .BuildServiceProvider();

        _validator = new RoutinesOptionsValidator(services.GetRequiredService<IServiceProviderIsKeyedService>());
    }

    private static RoutineOptions ValidRoutine(string name = "Job") => new()
    {
        Name = name,
        Group = "Routines",
        Service = "Example",
        CronExpression = "0 * * ? * *",
    };

    private IReadOnlyList<string> Validate(params RoutineOptions[] routines)
    {
        var options = new RoutinesOptions();
        options.Jobs.AddRange(routines);
        return _validator.Validate(null, options).Failures?.ToList() ?? [];
    }

    [Fact]
    public void ValidConfiguration_Passes()
    {
        Assert.Empty(Validate(ValidRoutine("A"), ValidRoutine("B")));
    }

    [Fact]
    public void InvalidCron_Fails()
    {
        var routine = ValidRoutine();
        routine.CronExpression = "0 61 * ? * *";

        Assert.Contains(Validate(routine), e => e.Contains("cron"));
    }

    [Fact]
    public void UnknownTimeZone_Fails()
    {
        var routine = ValidRoutine();
        routine.TimeZone = "Marte/Olympus";

        Assert.Contains(Validate(routine), e => e.Contains("zona horaria"));
    }

    [Fact]
    public void UnregisteredService_Fails()
    {
        var routine = ValidRoutine();
        routine.Service = "NoExiste";

        Assert.Contains(Validate(routine), e => e.Contains("NoExiste"));
    }

    [Fact]
    public void DuplicateIdentity_Fails()
    {
        Assert.Contains(Validate(ValidRoutine("A"), ValidRoutine("A")), e => e.Contains("Group/Name"));
    }

    [Fact]
    public void EmptyTimeZone_ResolvesToUtc()
    {
        Assert.Equal(TimeZoneInfo.Utc, ValidRoutine().ResolveTimeZone());
    }
}

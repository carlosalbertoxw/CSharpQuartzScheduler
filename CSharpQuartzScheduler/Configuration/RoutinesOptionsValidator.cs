using CSharpQuartzScheduler.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Quartz;

namespace CSharpQuartzScheduler.Configuration;

/// <summary>
/// Valida al arrancar que cada rutina tenga un cron válido, una zona horaria existente,
/// un servicio registrado y una identidad única. Así un error de configuración detiene
/// el host con un mensaje claro en lugar de fallar dentro de Quartz.
/// </summary>
public sealed class RoutinesOptionsValidator : IValidateOptions<RoutinesOptions>
{
    private readonly IServiceProviderIsKeyedService _keyedServices;

    public RoutinesOptionsValidator(IServiceProviderIsKeyedService keyedServices) => _keyedServices = keyedServices;

    public ValidateOptionsResult Validate(string? name, RoutinesOptions options)
    {
        var errors = new List<string>();
        var identities = new HashSet<(string, string)>();

        for (var i = 0; i < options.Jobs.Count; i++)
        {
            var routine = options.Jobs[i];
            var prefix = $"{RoutinesOptions.SectionName}[{i}] ({routine.Name})";

            if (string.IsNullOrWhiteSpace(routine.Name))
                errors.Add($"{prefix}: 'Name' es obligatorio.");

            if (string.IsNullOrWhiteSpace(routine.Group))
                errors.Add($"{prefix}: 'Group' es obligatorio.");

            if (!identities.Add((routine.Group, routine.Name)))
                errors.Add($"{prefix}: ya existe otra rutina con el mismo Group/Name.");

            if (string.IsNullOrWhiteSpace(routine.Service))
                errors.Add($"{prefix}: 'Service' es obligatorio.");
            else if (!_keyedServices.IsKeyedService(typeof(IRoutineService), routine.Service))
                errors.Add($"{prefix}: no hay un IRoutineService registrado con la clave '{routine.Service}'.");

            if (!CronExpression.TryParse(routine.CronExpression, out _))
                errors.Add($"{prefix}: la expresión cron '{routine.CronExpression}' no es válida.");

            if (!string.IsNullOrWhiteSpace(routine.TimeZone)
                && !TimeZoneInfo.TryFindSystemTimeZoneById(routine.TimeZone, out _))
                errors.Add($"{prefix}: la zona horaria '{routine.TimeZone}' no existe.");
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

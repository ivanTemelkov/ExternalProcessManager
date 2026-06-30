using System.Collections.Immutable;
using System.Globalization;

namespace IvTem.ExternalProcessManager.Configuration;

internal sealed class ExternalProcessConfigurationValidator
{
    private static TimeSpan DefaultMinBackoff => TimeSpan.FromSeconds(2);

    private static TimeSpan DefaultMaxBackoff => TimeSpan.FromMinutes(1);

    private static TimeSpan DefaultStableRunDuration => TimeSpan.FromMinutes(5);

    private static TimeSpan DefaultGracefulStopTimeout => TimeSpan.FromSeconds(10);

    public EffectiveExternalProcessManagerConfiguration Validate(RawExternalProcessManagerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Dictionary<string, int> aliasCounts = BuildAliasCounts(configuration.Processes);
        List<EffectiveExternalProcessConfiguration> processes = [];
        List<InvalidExternalProcessConfiguration> invalidProcesses = [];
        List<ExternalProcessValidationError> validationErrors = [];

        foreach (RawExternalProcessConfiguration process in configuration.Processes)
        {
            ValidatedProcess validatedProcess = ValidateProcess(process, aliasCounts);

            if (validatedProcess.EffectiveConfiguration is not null)
            {
                processes.Add(validatedProcess.EffectiveConfiguration);
                continue;
            }

            InvalidExternalProcessConfiguration invalidProcess = new()
            {
                Alias = validatedProcess.Alias,
                Path = process.Path,
                ValidationErrors = [.. validatedProcess.ValidationErrors],
            };

            invalidProcesses.Add(invalidProcess);
            validationErrors.AddRange(validatedProcess.ValidationErrors);
        }

        return new EffectiveExternalProcessManagerConfiguration
        {
            Processes = [.. processes],
            InvalidProcesses = [.. invalidProcesses],
            ValidationErrors = [.. validationErrors],
        };
    }

    private static Dictionary<string, int> BuildAliasCounts(ImmutableArray<RawExternalProcessConfiguration> processes)
    {
        Dictionary<string, int> aliasCounts = new(StringComparer.OrdinalIgnoreCase);

        foreach (RawExternalProcessConfiguration process in processes)
        {
            string? alias = NormalizeRequiredValue(process.Alias.Value);

            if (alias is null)
                continue;

            aliasCounts[alias] = aliasCounts.GetValueOrDefault(alias) + 1;
        }

        return aliasCounts;
    }

    private static ValidatedProcess ValidateProcess(
        RawExternalProcessConfiguration process,
        IReadOnlyDictionary<string, int> aliasCounts)
    {
        List<ExternalProcessValidationError> validationErrors = [];
        string? alias = ValidateAlias(process, aliasCounts, validationErrors);
        string? fileName = ValidateRequiredValue(
            process.FileName,
            alias,
            "FileName is required.",
            validationErrors);
        EffectiveRestartConfiguration restart = ValidateRestart(process, alias, validationErrors);

        if (alias is null || fileName is null || validationErrors.Count > 0)
            return new ValidatedProcess(alias, EffectiveConfiguration: null, [.. validationErrors]);

        return new ValidatedProcess(
            alias,
            new EffectiveExternalProcessConfiguration
            {
                Path = process.Path,
                Alias = alias,
                AliasKey = alias,
                FileName = fileName,
                ArgumentMode = GetArgumentMode(process),
                Arguments = GetArguments(process),
                ArgumentList = GetArgumentList(process),
                WorkingDirectory = NormalizeOptionalValue(process.WorkingDirectory.Value),
                Environment = NormalizeEnvironment(process.Environment),
                Restart = restart,
                ScheduledRestarts = NormalizeScheduledRestarts(process.ScheduledRestarts),
            },
            []);
    }

    private static string? ValidateAlias(
        RawExternalProcessConfiguration process,
        IReadOnlyDictionary<string, int> aliasCounts,
        List<ExternalProcessValidationError> validationErrors)
    {
        string? alias = ValidateRequiredValue(
            process.Alias,
            alias: null,
            "Alias is required.",
            validationErrors);

        if (alias is null)
            return null;

        if (aliasCounts.TryGetValue(alias, out int count) && count > 1)
        {
            validationErrors.Add(new ExternalProcessValidationError
            {
                Alias = alias,
                Path = process.Alias.Path,
                Message = "Alias must be unique.",
            });
        }

        return alias;
    }

    private static string? ValidateRequiredValue(
        RawConfigurationValue value,
        string? alias,
        string message,
        List<ExternalProcessValidationError> validationErrors)
    {
        string? normalizedValue = NormalizeRequiredValue(value.Value);

        if (normalizedValue is not null)
            return normalizedValue;

        validationErrors.Add(new ExternalProcessValidationError
        {
            Alias = alias,
            Path = value.Path,
            Message = message,
        });

        return null;
    }

    private static EffectiveRestartConfiguration ValidateRestart(
        RawExternalProcessConfiguration process,
        string? alias,
        List<ExternalProcessValidationError> validationErrors)
    {
        RawRestartConfiguration? restart = process.Restart;
        ExternalProcessRestartMode mode = ValidateRestartMode(restart?.Mode, alias, validationErrors);
        TimeSpan minBackoff = ValidateDuration(
            restart?.MinBackoff,
            alias,
            nameof(EffectiveRestartConfiguration.MinBackoff),
            DefaultMinBackoff,
            validationErrors);
        TimeSpan maxBackoff = ValidateDuration(
            restart?.MaxBackoff,
            alias,
            nameof(EffectiveRestartConfiguration.MaxBackoff),
            DefaultMaxBackoff,
            validationErrors);
        TimeSpan stableRunDuration = ValidateDuration(
            restart?.StableRunDuration,
            alias,
            nameof(EffectiveRestartConfiguration.StableRunDuration),
            DefaultStableRunDuration,
            validationErrors);
        TimeSpan gracefulStopTimeout = ValidateDuration(
            restart?.GracefulStopTimeout,
            alias,
            nameof(EffectiveRestartConfiguration.GracefulStopTimeout),
            DefaultGracefulStopTimeout,
            validationErrors);

        if (minBackoff > maxBackoff)
        {
            validationErrors.Add(new ExternalProcessValidationError
            {
                Alias = alias,
                Path = restart?.MinBackoff.Path ?? process.Path,
                Message = "MinBackoff must be less than or equal to MaxBackoff.",
            });
        }

        return new EffectiveRestartConfiguration
        {
            Mode = mode,
            MinBackoff = minBackoff,
            MaxBackoff = maxBackoff,
            StableRunDuration = stableRunDuration,
            GracefulStopTimeout = gracefulStopTimeout,
        };
    }

    private static ExternalProcessRestartMode ValidateRestartMode(
        RawConfigurationValue? modeValue,
        string? alias,
        List<ExternalProcessValidationError> validationErrors)
    {
        string? rawMode = NormalizeRequiredValue(modeValue?.Value);

        if (rawMode is null)
            return ExternalProcessRestartMode.NonZeroExitCode;

        if (TryParseRestartMode(rawMode, out ExternalProcessRestartMode mode))
            return mode;

        validationErrors.Add(new ExternalProcessValidationError
        {
            Alias = alias,
            Path = modeValue?.Path ?? string.Empty,
            Message = "Restart mode must be NonZeroExitCode, Always, or Never.",
        });

        return ExternalProcessRestartMode.NonZeroExitCode;
    }

    private static bool TryParseRestartMode(string value, out ExternalProcessRestartMode mode)
    {
        if (string.Equals(value, nameof(ExternalProcessRestartMode.NonZeroExitCode), StringComparison.OrdinalIgnoreCase))
        {
            mode = ExternalProcessRestartMode.NonZeroExitCode;
            return true;
        }

        if (string.Equals(value, nameof(ExternalProcessRestartMode.Always), StringComparison.OrdinalIgnoreCase))
        {
            mode = ExternalProcessRestartMode.Always;
            return true;
        }

        if (string.Equals(value, nameof(ExternalProcessRestartMode.Never), StringComparison.OrdinalIgnoreCase))
        {
            mode = ExternalProcessRestartMode.Never;
            return true;
        }

        mode = ExternalProcessRestartMode.NonZeroExitCode;
        return false;
    }

    private static TimeSpan ValidateDuration(
        RawConfigurationValue? durationValue,
        string? alias,
        string name,
        TimeSpan defaultValue,
        List<ExternalProcessValidationError> validationErrors)
    {
        string? rawDuration = NormalizeRequiredValue(durationValue?.Value);

        if (rawDuration is null)
            return defaultValue;

        if (TimeSpan.TryParse(rawDuration, CultureInfo.InvariantCulture, out TimeSpan duration) == false)
        {
            validationErrors.Add(new ExternalProcessValidationError
            {
                Alias = alias,
                Path = durationValue?.Path ?? string.Empty,
                Message = $"{name} must be a valid TimeSpan.",
            });

            return defaultValue;
        }

        if (duration <= TimeSpan.Zero)
        {
            validationErrors.Add(new ExternalProcessValidationError
            {
                Alias = alias,
                Path = durationValue?.Path ?? string.Empty,
                Message = $"{name} must be greater than zero.",
            });

            return defaultValue;
        }

        return duration;
    }

    private static EffectiveProcessArgumentMode GetArgumentMode(RawExternalProcessConfiguration process)
    {
        if (process.ArgumentList.IsEmpty == false)
            return EffectiveProcessArgumentMode.ArgumentList;

        if (process.Arguments.Value is not null)
            return EffectiveProcessArgumentMode.RawString;

        return EffectiveProcessArgumentMode.None;
    }

    private static string? GetArguments(RawExternalProcessConfiguration process)
    {
        if (process.ArgumentList.IsEmpty == false)
            return null;

        return process.Arguments.Value;
    }

    private static ImmutableArray<string> GetArgumentList(RawExternalProcessConfiguration process)
    {
        if (process.ArgumentList.IsEmpty)
            return [];

        return [.. process.ArgumentList.Select(value => value.Value ?? string.Empty)];
    }

    private static ImmutableDictionary<string, string> NormalizeEnvironment(
        ImmutableDictionary<string, RawConfigurationValue> environment)
        => environment.ToImmutableDictionary(
            item => item.Key,
            item => item.Value.Value ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);

    private static ImmutableArray<EffectiveScheduledRestartConfiguration> NormalizeScheduledRestarts(
        ImmutableArray<RawScheduledRestartConfiguration> scheduledRestarts)
        =>
        [
            .. scheduledRestarts.Select(schedule => new EffectiveScheduledRestartConfiguration
            {
                Path = schedule.Path,
                HourOfDay = NormalizeOptionalValue(schedule.HourOfDay.Value),
                DayOfWeek = NormalizeOptionalValue(schedule.DayOfWeek.Value),
                DayOfWeekValues = [.. schedule.DayOfWeekValues.Select(value => value.Value ?? string.Empty)],
            }),
        ];

    private static string? NormalizeRequiredValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private sealed record ValidatedProcess(
        string? Alias,
        EffectiveExternalProcessConfiguration? EffectiveConfiguration,
        ImmutableArray<ExternalProcessValidationError> ValidationErrors);
}

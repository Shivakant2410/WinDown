using ClamDown.Core.Models;

namespace ClamDown.Core.Safety;

/// <summary>
/// Conservative session guard. Missing/stale sensor data never counts as safe.
/// Temperature cutoffs are configurable policy values, not universal silicon limits.
/// </summary>
public static class SafetyEvaluator
{
    public static SafetyDecision Evaluate(
        SessionOptions options,
        SystemTelemetry telemetry,
        DateTimeOffset now)
    {
        if (options.BatteryCutoffPercent is < 0 or > 50)
            throw new ArgumentOutOfRangeException(nameof(options.BatteryCutoffPercent));
        if (options.ThermalCutoffCelsius is < 60 or > 110)
            throw new ArgumentOutOfRangeException(nameof(options.ThermalCutoffCelsius));

        if (options.Duration is { } duration &&
            options.StartedAt is { } startedAt &&
            now >= startedAt + duration)
            return new(true, SessionStopReason.TimerExpired, "Session duration has elapsed.");

        if (telemetry.CapturedAt > now || now - telemetry.CapturedAt > TimeSpan.FromSeconds(15))
        {
            return options.RequireValidThermalSensor
                ? new(true, SessionStopReason.SensorUnavailable, "Telemetry is stale; stopping conservatively.")
                : new(false, null, "Telemetry is stale; thermal protection is degraded.");
        }

        if (telemetry.IsOnAcPower == false &&
            telemetry.BatteryPercent is { } battery &&
            battery <= options.BatteryCutoffPercent)
        {
            return new(true, SessionStopReason.BatteryCutoff, $"Battery reached {battery}%.");
        }

        if (options.RequireValidThermalSensor && telemetry.CpuTemperatureC is null)
            return new(true, SessionStopReason.SensorUnavailable, "No valid CPU temperature sensor is available.");

        if (telemetry.CpuTemperatureC is { } cpu && cpu >= options.ThermalCutoffCelsius)
            return new(true, SessionStopReason.ThermalCutoff, $"CPU temperature reached {cpu:F1} °C.");

        if (telemetry.GpuTemperatureC is { } gpu && gpu >= options.ThermalCutoffCelsius)
            return new(true, SessionStopReason.ThermalCutoff, $"GPU temperature reached {gpu:F1} °C.");

        return new(false, null, "Telemetry is within configured session limits.");
    }
}

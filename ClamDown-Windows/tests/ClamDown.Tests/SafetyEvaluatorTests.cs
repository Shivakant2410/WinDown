using ClamDown.Core.Models;
using ClamDown.Core.Safety;
using Xunit;

namespace ClamDown.Tests;

public class SafetyEvaluatorTests
{
    private static SystemTelemetry Telemetry(
        DateTimeOffset now, int? battery = 80, bool? ac = true,
        double? cpu = 55, double? gpu = 50) =>
        new(now, battery, ac, cpu, gpu, Array.Empty<SensorReading>(), "test", Array.Empty<string>());

    [Fact]
    public void StopsWhenBatteryCutoffReachedOnBattery()
    {
        var now = DateTimeOffset.UtcNow;
        var result = SafetyEvaluator.Evaluate(new(Duration: null, BatteryCutoffPercent: 10, ThermalCutoffCelsius: 90), Telemetry(now, 9, false), now);
        Assert.True(result.ShouldStop);
        Assert.Equal(SessionStopReason.BatteryCutoff, result.Reason);
    }

    [Fact]
    public void DoesNotApplyBatteryCutoffOnAcPower()
    {
        var now = DateTimeOffset.UtcNow;
        var result = SafetyEvaluator.Evaluate(new(Duration: null, BatteryCutoffPercent: 10, ThermalCutoffCelsius: 90), Telemetry(now, 5, true), now);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void StopsWhenCpuTemperatureReachesCutoff()
    {
        var now = DateTimeOffset.UtcNow;
        var result = SafetyEvaluator.Evaluate(new(Duration: null, BatteryCutoffPercent: 10, ThermalCutoffCelsius: 90), Telemetry(now, cpu: 90), now);
        Assert.Equal(SessionStopReason.ThermalCutoff, result.Reason);
    }

    [Fact]
    public void StopsWhenRequiredSensorIsMissing()
    {
        var now = DateTimeOffset.UtcNow;
        var result = SafetyEvaluator.Evaluate(new(Duration: null, BatteryCutoffPercent: 10, ThermalCutoffCelsius: 90), Telemetry(now, cpu: null), now);
        Assert.Equal(SessionStopReason.SensorUnavailable, result.Reason);
    }

    [Fact]
    public void StopsWhenTelemetryIsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var result = SafetyEvaluator.Evaluate(new(Duration: null, BatteryCutoffPercent: 10, ThermalCutoffCelsius: 90), Telemetry(now.AddSeconds(-30)), now);
        Assert.Equal(SessionStopReason.SensorUnavailable, result.Reason);
    }
}

namespace ClamDown.Core.Models;

public sealed record SensorReading(
    string SensorId,
    string Name,
    double Value,
    string Unit,
    DateTimeOffset CapturedAt,
    bool IsValid,
    string? Source = null);

public sealed record SystemTelemetry(
    DateTimeOffset CapturedAt,
    int? BatteryPercent,
    bool? IsOnAcPower,
    double? CpuTemperatureC,
    double? GpuTemperatureC,
    IReadOnlyList<SensorReading> Fans,
    string? HardwareProvider,
    IReadOnlyList<string> Warnings);

public enum SessionStopReason
{
    UserRequested,
    TimerExpired,
    BatteryCutoff,
    ThermalCutoff,
    SensorUnavailable,
    ServiceStopping,
    WatchdogRecovery
}

public sealed record SessionOptions(
    TimeSpan? Duration,
    DateTimeOffset? StartedAt = null,
    int BatteryCutoffPercent = 10,
    double ThermalCutoffCelsius = 90,
    bool RequireValidThermalSensor = true);

public sealed record SafetyDecision(bool ShouldStop, SessionStopReason? Reason, string Explanation);

namespace ClamDown.Core.IPC;

public sealed record ControlRequest(string Command, int? DurationSeconds = null);
public sealed record ControlResponse(bool Success, string State, string Message, DateTimeOffset? EndsAt = null);

using ClamDown.Core.Power;

namespace ClamDown.Service;

/// <summary>
/// Owns one bounded Windows execution-state request. This is not a thermal
/// safety controller; it never changes lid policy or fan settings.
/// </summary>
public sealed class SessionController(IPowerRequest powerRequest, ILogger<SessionController> logger)
{
    public const int MinDurationSeconds = 60;
    public const int MaxDurationSeconds = 4 * 60 * 60;

    private readonly object _gate = new();
    private CancellationTokenSource? _expiry;
    private DateTimeOffset? _endsAt;
    private bool _active;

    public (bool Active, DateTimeOffset? EndsAt) Snapshot()
    {
        lock (_gate) return (_active, _endsAt);
    }

    public bool Start(int durationSeconds, out DateTimeOffset? endsAt, out string message)
    {
        if (durationSeconds < MinDurationSeconds || durationSeconds > MaxDurationSeconds)
        {
            endsAt = null;
            message = $"Duration must be between {MinDurationSeconds} seconds and {MaxDurationSeconds} seconds.";
            return false;
        }

        lock (_gate)
        {
            if (_active)
            {
                endsAt = _endsAt;
                message = "A keep-awake session is already active.";
                return true;
            }

            try
            {
                powerRequest.KeepSystemAwake("ClamDown bounded keep-awake session");
                _expiry = new CancellationTokenSource();
                _endsAt = DateTimeOffset.UtcNow.AddSeconds(durationSeconds);
                _active = true;
                var token = _expiry.Token;
                _ = ExpireAsync(durationSeconds, token);
                endsAt = _endsAt;
                message = "Keep-awake session started. Windows lid-close policy is unchanged.";
                logger.LogInformation("Keep-awake session started; expires at {EndsAt}.", _endsAt);
                return true;
            }
            catch (Exception ex)
            {
                powerRequest.Release();
                endsAt = null;
                message = $"Could not activate the Windows power request: {ex.Message}";
                logger.LogError(ex, "Could not start keep-awake session.");
                return false;
            }
        }
    }

    public void Stop(string reason = "User requested")
    {
        lock (_gate)
        {
            if (!_active) return;
            _active = false;
            _endsAt = null;
            _expiry?.Cancel();
            _expiry?.Dispose();
            _expiry = null;
            powerRequest.Release();
            logger.LogInformation("Keep-awake session stopped: {Reason}.", reason);
        }
    }

    private async Task ExpireAsync(int seconds, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);
            Stop("Timer expired");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
}

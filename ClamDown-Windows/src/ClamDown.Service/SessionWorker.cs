namespace ClamDown.Service;

/// <summary>Ensures the active power request is always released during shutdown.</summary>
public sealed class SessionWorker(
    ILogger<SessionWorker> logger,
    SessionController sessions) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ClamDown service started.");
        try { await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { sessions.Stop("Service stopping"); }
    }
}

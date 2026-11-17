using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using ClamDown.Core.IPC;

namespace ClamDown.Service;

/// <summary>
/// Local named-pipe endpoint. The pipe DACL grants access only to authenticated
/// local users and LocalSystem. The command surface is intentionally tiny.
/// </summary>
public sealed class ControlPipeWorker(
    SessionController sessions,
    ILogger<ControlPipeWorker> logger) : BackgroundService
{
    private const string PipeName = "ClamDown.Control";
    private const int MaxMessageLength = 4096;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ClamDown local control pipe is starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var pipe = CreateServer();
                await pipe.WaitForConnectionAsync(stoppingToken);
                await HandleClientAsync(pipe, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Control pipe client failed.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private static NamedPipeServerStream CreateServer()
    {
        var security = new PipeSecurity();
        var authenticatedUsers = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        var localSystem = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
        security.AddAccessRule(new PipeAccessRule(authenticatedUsers, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(localSystem, PipeAccessRights.FullControl, AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous, MaxMessageLength, MaxMessageLength, security);
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
        var line = await reader.ReadLineAsync(cancellationToken);
        if (line is null || line.Length > MaxMessageLength)
        {
            await WriteAsync(writer, new(false, "error", "Invalid or oversized request."), cancellationToken);
            return;
        }

        ControlRequest? request;
        try { request = JsonSerializer.Deserialize<ControlRequest>(line, JsonOptions); }
        catch (JsonException)
        {
            await WriteAsync(writer, new(false, "error", "Malformed request."), cancellationToken);
            return;
        }

        if (request is null)
        {
            await WriteAsync(writer, new(false, "error", "Empty request."), cancellationToken);
            return;
        }

        ControlResponse response = request.Command.ToLowerInvariant() switch
        {
            "start" when request.DurationSeconds is int seconds => Start(seconds),
            "stop" => Stop(),
            "status" => Status(),
            _ => new(false, "error", "Supported commands are start, stop, and status.")
        };
        await WriteAsync(writer, response, cancellationToken);
    }

    private ControlResponse Start(int seconds)
    {
        var ok = sessions.Start(seconds, out var endsAt, out var message);
        return new(ok, ok ? "active" : "stopped", message, endsAt);
    }

    private ControlResponse Stop()
    {
        sessions.Stop();
        return Status("Session stopped.");
    }

    private ControlResponse Status(string? message = null)
    {
        var state = sessions.Snapshot();
        return new(true, state.Active ? "active" : "stopped",
            message ?? (state.Active ? "Keep-awake session is active." : "No keep-awake session is active."),
            state.EndsAt);
    }

    private static Task WriteAsync(StreamWriter writer, ControlResponse response, CancellationToken token) =>
        writer.WriteLineAsync(JsonSerializer.Serialize(response, JsonOptions).AsMemory(), token);
}

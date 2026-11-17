using System.IO.Pipes;
using System.Text.Json;
using ClamDown.Core.IPC;

namespace ClamDown.Desktop.IPC;

public sealed class ServiceClient
{
    private const string PipeName = "ClamDown.Control";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ControlResponse> SendAsync(ControlRequest request, CancellationToken cancellationToken = default)
    {
        using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(1500, cancellationToken);
        using var writer = new StreamWriter(pipe, leaveOpen: true) { AutoFlush = true };
        using var reader = new StreamReader(pipe, leaveOpen: true);
        var json = JsonSerializer.Serialize(request, JsonOptions);
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        var line = await reader.ReadLineAsync(cancellationToken);
        if (line is null || line.Length > 4096)
            throw new IOException("The ClamDown service returned an invalid response.");
        return JsonSerializer.Deserialize<ControlResponse>(line, JsonOptions)
            ?? throw new IOException("The ClamDown service returned an empty response.");
    }
}

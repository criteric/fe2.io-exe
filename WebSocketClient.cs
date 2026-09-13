using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FE2IONative;

public sealed class Fe2IoClient : IDisposable
{
    private readonly Uri _serverUri;
    private readonly string _username;
    private readonly ClientWebSocket _socket = new();

    public event Func<ServerMessage, Task>? MessageReceived;

    public Fe2IoClient(string server, string username)
    {
        _serverUri = new Uri(server);
        _username = username;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _socket.ConnectAsync(_serverUri, ct);
        Console.WriteLine("Connection established");

        // Same handshake as mini-fe2io: first message sent is the raw username.
        var payload = Encoding.UTF8.GetBytes(_username);
        await _socket.SendAsync(payload, WebSocketMessageType.Text, true, ct);
        Console.WriteLine("Sent username to server");
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var buffer = new byte[8192];

        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await _socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, ct);
                    return;
                }
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            var text = Encoding.UTF8.GetString(ms.ToArray());
            Console.WriteLine($"Got message {text}");

            ServerMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<ServerMessage>(text);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"Error parsing JSON: {ex.Message}");
                continue;
            }

            if (message is not null && MessageReceived is not null)
                await MessageReceived.Invoke(message);
        }
    }

    public void Dispose() => _socket.Dispose();
}

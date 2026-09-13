using System.Text.Json.Serialization;

namespace FE2IONative;

// Mirrors the Rust "MessageFormat" struct from mini-fe2io's json_processor.rs.
// Server sends camelCase JSON, e.g.:
//   {"msgType":"bgm","audioUrl":"https://.../track.mp3"}
//   {"msgType":"gameStatus","statusType":"died"}
//   {"msgType":"gameStatus","statusType":"left"}
public sealed class ServerMessage
{
    [JsonPropertyName("msgType")]
    public string? MsgType { get; set; }

    [JsonPropertyName("statusType")]
    public string? StatusType { get; set; }

    [JsonPropertyName("audioUrl")]
    public string? AudioUrl { get; set; }
}

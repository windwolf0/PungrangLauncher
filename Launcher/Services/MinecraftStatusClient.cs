using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PungrangLauncher.Models;

namespace PungrangLauncher.Services;

public sealed class MinecraftStatusClient
{
    public async Task<ServerInfo> QueryAsync(
        string host, int port, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, timeout.Token);
        await using NetworkStream stream = client.GetStream();

        using var handshake = new MemoryStream();
        WriteVarInt(handshake, 0);
        WriteVarInt(handshake, -1);
        WriteString(handshake, host);
        handshake.WriteByte((byte)(port >> 8));
        handshake.WriteByte((byte)port);
        WriteVarInt(handshake, 1);
        await WritePacketAsync(stream, handshake.ToArray(), timeout.Token);
        await WritePacketAsync(stream, [0], timeout.Token);

        int packetLength = await ReadVarIntAsync(stream, timeout.Token);
        if (packetLength is < 1 or > 1_048_576)
            throw new InvalidDataException("Invalid status packet length.");
        byte[] packet = new byte[packetLength];
        await stream.ReadExactlyAsync(packet, timeout.Token);
        int offset = 0;
        if (ReadVarInt(packet, ref offset) != 0)
            throw new InvalidDataException("Unexpected packet ID.");
        int jsonLength = ReadVarInt(packet, ref offset);
        if (jsonLength < 1 || jsonLength > packet.Length - offset)
            throw new InvalidDataException("Invalid status JSON length.");

        using var json = JsonDocument.Parse(packet.AsMemory(offset, jsonLength));
        var root = json.RootElement;
        var players = root.GetProperty("players");
        return new ServerInfo
        {
            Online = true,
            OnlinePlayers = players.GetProperty("online").GetInt32(),
            MaxPlayers = players.GetProperty("max").GetInt32(),
            Motd = ReadText(root.GetProperty("description")),
            CheckedAt = DateTimeOffset.Now
        };
    }

    private static string ReadText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return element.GetString() ?? "";
        if (element.ValueKind == JsonValueKind.Array)
            return string.Concat(element.EnumerateArray().Select(ReadText));
        if (element.ValueKind == JsonValueKind.Object)
        {
            string own = element.TryGetProperty("text", out var text) ? text.GetString() ?? "" : "";
            string extra = element.TryGetProperty("extra", out var children) ? ReadText(children) : "";
            return own + extra;
        }
        return "";
    }

    private static void WriteString(Stream s, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        WriteVarInt(s, bytes.Length);
        s.Write(bytes);
    }

    private static void WriteVarInt(Stream s, int value)
    {
        uint v = unchecked((uint)value);
        while ((v & 0xFFFFFF80) != 0) { s.WriteByte((byte)((v & 0x7F) | 0x80)); v >>= 7; }
        s.WriteByte((byte)v);
    }

    private static int ReadVarInt(byte[] data, ref int offset)
    {
        int result = 0;
        for (int i = 0; i < 5; i++)
        {
            if (offset >= data.Length) throw new EndOfStreamException();
            byte b = data[offset++];
            result |= (b & 0x7F) << (7 * i);
            if ((b & 0x80) == 0) return result;
        }
        throw new InvalidDataException("Malformed VarInt.");
    }

    private static async Task<int> ReadVarIntAsync(Stream s, CancellationToken ct)
    {
        int result = 0;
        for (int i = 0; i < 5; i++)
        {
            byte[] b = new byte[1];
            await s.ReadExactlyAsync(b, ct);
            result |= (b[0] & 0x7F) << (7 * i);
            if ((b[0] & 0x80) == 0) return result;
        }
        throw new InvalidDataException("Malformed VarInt.");
    }

    private static async Task WritePacketAsync(Stream s, byte[] payload, CancellationToken ct)
    {
        using var packet = new MemoryStream();
        WriteVarInt(packet, payload.Length);
        packet.Write(payload);
        await s.WriteAsync(packet.ToArray(), ct);
    }
}

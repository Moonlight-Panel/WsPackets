namespace WsPackets.Test.Server.Packets;

public class Pong
{
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
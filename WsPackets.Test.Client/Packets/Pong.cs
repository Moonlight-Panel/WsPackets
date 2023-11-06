namespace WsPackets.Test.Client.Packets;

public class Pong
{
    public DateTime Time { get; set; } = DateTime.UtcNow;
}
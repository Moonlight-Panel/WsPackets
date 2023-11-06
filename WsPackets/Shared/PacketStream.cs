using System.Text;

namespace WsPackets.Shared;

public class PacketStream
{
    private readonly Stream Stream;

    public PacketStream(Stream stream)
    {
        Stream = stream;
    }

    public async Task<byte[]> Read(int lenght, int offset = 0)
    {
        byte[] buffer = new byte[lenght];
        _ = await Stream.ReadAsync(buffer, 0, lenght);
        return buffer;
    }

    public async Task<int> ReadInt()
    {
        return BitConverter.ToInt32(await Read(4));
    }

    public async Task<string> ReadString()
    {
        var lenght = await ReadInt();
        var buffer = await Read(lenght);

        return Encoding.UTF8.GetString(buffer);
    }

    public async Task Write(byte[] buffer)
    {
        await Stream.WriteAsync(buffer);
    }
    
    public async Task WriteInt(int i)
    {
        await Write(BitConverter.GetBytes(i));
    }

    public async Task WriteString(string s)
    {
        var buffer = Encoding.UTF8.GetBytes(s);

        await WriteInt(buffer.Length);
        await Write(buffer);
    }
}
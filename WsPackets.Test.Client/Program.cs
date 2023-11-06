using System.Reflection;
using Serilog;
using WsPackets.Client;
using WsPackets.Shared;
using WsPackets.Test.Client.Packets;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

Log.Information("Creating client");

var client = new WspClient("ws://localhost:5022/ws", new AssemblyTypeResolver(
    Assembly.GetExecutingAssembly(),
    "WsPackets.Test.Client.Packets"
));

client.OnPacket += (_, packet) =>
{
    if (packet is Pong pong)
    {
        Log.Information($"Pong: {pong.Time}");
    }
};

client.OnConnectionLost += (_, _) =>
{
    Log.Information("Connection lost");
};

Log.Information("Addng connection");
await client.AddConnection();

Log.Information("Sendings packets");
for (int i = 0; i < 1000000; i++)
{
    await client.Send(new Ping());
}

await Task.Delay(-1);
using System.Reflection;
using Serilog;
using WsPackets.Server;
using WsPackets.Shared;
using WsPackets.Test.Server.Packets;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var wspServer = new WspServer(new AssemblyTypeResolver(
    Assembly.GetExecutingAssembly(),
    "WsPackets.Test.Server.Packets"
));

wspServer.OnPacket += async (_, packet) =>
{
    if (packet is Ping ping)
    {
        Log.Information($"Ping!. {ping.Lol}");

        await wspServer.Send(new Pong());
    }
};


wspServer.OnConnectionLost += (_, _) =>
{
    Log.Information("Connection lost");
};

builder.Services.AddSingleton(wspServer);

builder.Services.AddControllers();

var app = builder.Build();

app.UseWebSockets();
app.UseAuthorization();

app.MapControllers();

app.Run();
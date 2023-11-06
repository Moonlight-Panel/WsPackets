using Microsoft.AspNetCore.Mvc;
using WsPackets.Server;

namespace WsPackets.Test.Server.Controllers;

public class WsController : Controller
{
    private readonly WspServer WspServer;

    public WsController(WspServer wspServer)
    {
        WspServer = wspServer;
    }

    [Route("/ws")]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine("Incoming ws connection");
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connection = await WspServer.AddConnection(webSocket);
            Console.WriteLine("Waiting for completion");
            await connection.WaitForClose();
            Console.WriteLine("Completed");
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}
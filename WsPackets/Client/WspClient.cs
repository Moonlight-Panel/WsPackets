using System.Net.WebSockets;
using Serilog;
using WsPackets.Shared;

namespace WsPackets.Client;

public class WspClient
{
    private readonly string Endpoint;
    private readonly ITypeResolver TypeResolver;
    private readonly List<WspConnection> Connections = new();
    private readonly CancellationTokenSource CancellationToken = new();
    
    // Events
    public EventHandler<object> OnPacket { get; set; }
    public EventHandler OnConnectionLost { get; set; }

    public WspClient(string endpoint, ITypeResolver typeResolver)
    {
        Endpoint = endpoint;
        TypeResolver = typeResolver;
    }

    public async Task<WspConnection> AddConnection(Action<ClientWebSocketOptions>? modifyOptions = null)
    {
        var webSocket = new ClientWebSocket();
        
        if(modifyOptions != null)
            modifyOptions.Invoke(webSocket.Options);
        
        await webSocket.ConnectAsync(new Uri(Endpoint), CancellationToken.Token);

        if (webSocket.State == WebSocketState.Closed || webSocket.State == WebSocketState.Aborted)
            throw new Exception("Unable to connect to endpoint");
        
        var wspConnection = new WspConnection(webSocket, TypeResolver);

        //TODO: replace
        wspConnection.OnLogError = Log.Error;

        wspConnection.OnDisconnected += (_, _) =>
        {
            lock (Connections)
                Connections.Remove(wspConnection);
            
            OnConnectionLost?.Invoke(null, null!);
        };

        wspConnection.OnPacket += (_, packet) =>
        {
            OnPacket?.Invoke(null, packet);
        };

        lock (Connections)
            Connections.Add(wspConnection);

        await wspConnection.Start();

        return wspConnection;
    }

    public async Task Close()
    {
        WspConnection[] connections;

        lock (Connections)
            connections = Connections.ToArray();

        foreach (var connection in connections)
            await connection.Close();
        
        CancellationToken.Cancel();

        lock (Connections)
            Connections.Clear();
    }

    public WspConnection? GetBestConnection()
    {
        lock (Connections)
        {
            if (Connections.Count == 0)
                return null;
            
            return Connections.MinBy(x => x.GetWriteQueueLenght());
        }
    }

    public async Task Send(object packet)
    {
        var connection = GetBestConnection();

        if (connection == null)
            throw new Exception("Unable to find any connection. Make sure at least one connection is open");

        await connection.Send(packet);
    }

    public int GetConnectionsCount()
    {
        lock (Connections)
        {
            return Connections.Count;
        }
    }
}
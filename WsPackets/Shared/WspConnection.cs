using System.Diagnostics;
using System.Net.WebSockets;
using Newtonsoft.Json;
using Serilog;

namespace WsPackets.Shared;

public class WspConnection
{
    //
    private readonly WebSocket WebSocket;
    private readonly ITypeResolver TypeResolver;

    // Events
    public Action<string>? OnLogError { get; set; } //TODO: Find a better solution
    public EventHandler<object> OnPacket { get; set; }
    public EventHandler OnDisconnected { get; set; }

    // Sources
    private readonly CancellationTokenSource CancellationToken = new();
    private readonly TaskCompletionSource TaskCompletion = new();

    // Queues
    private readonly Queue<object> ProcessQueue = new();
    private readonly Queue<object> WriteQueue = new();

    // Constants
    private const int ReceiveBufferSize = 1024;

    public WspConnection(WebSocket webSocket, ITypeResolver typeResolver)
    {
        WebSocket = webSocket;
        TypeResolver = typeResolver;
    }

    #region Loops

    private async Task ReadLoop()
    {
        while (WebSocket.State == WebSocketState.Open)
        {
            try
            {
                // Setup buffer and read
                var buffer = new byte[ReceiveBufferSize];
                var readResult = await WebSocket.ReceiveAsync(buffer, CancellationToken.Token);

                // Create new buffer with the correct size
                var correctSizeBuffer = new byte[readResult.Count];

                // Copy value from receive buffer into correctly sized buffer. Delete old buffer
                Array.Copy(buffer, correctSizeBuffer, readResult.Count);
                buffer = Array.Empty<byte>();

                // Create packet stream
                var memoryStream = new MemoryStream(correctSizeBuffer);
                var packetStream = new PacketStream(memoryStream);

                // Parse header and resolve type
                var typeName = await packetStream.ReadString();
                var type = TypeResolver.ResolveType(typeName);

                if (type == null) // Unknown type. Cleaning up
                {
                    OnLogError?.Invoke($"Unknown type '{typeName}'");
                    await memoryStream.DisposeAsync();
                    continue;
                }

                // Parse packet
                var json = await packetStream.ReadString();
                object? packet = JsonConvert.DeserializeObject(json, type);

                if (packet == null) // Unable to parse
                {
                    OnLogError?.Invoke($"Unable to parse type '{typeName}'");
                    await memoryStream.DisposeAsync();
                    continue;
                }

                // Add packet to queue
                lock (ProcessQueue)
                    ProcessQueue.Enqueue(packet);

                // Cleanup
                await memoryStream.DisposeAsync();
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"Error in read loop: {e.Message}");
            }
        }

        await Close();
    }

    private async Task WriteLoop()
    {
        while (WebSocket.State == WebSocketState.Open)
        {
            try
            {
                // Select packet if any
                object? packet = null;

                lock (WriteQueue)
                {
                    if (WriteQueue.Count > 0)
                        packet = WriteQueue.Dequeue();
                }

                if (packet == null)
                {
                    // No packet found, wait a little bit and try again
                    await Task.Delay(10);
                    continue;
                }

                // Setup streams
                var memoryStream = new MemoryStream();
                var packetStream = new PacketStream(memoryStream);

                // Resolve name and write header
                var typeName = TypeResolver.ResolveName(packet.GetType());

                if (typeName == null)
                {
                    OnLogError?.Invoke($"Unknown type '{typeName}'");
                    await memoryStream.DisposeAsync();
                    continue;
                }

                await packetStream.WriteString(typeName);

                // Parse and write packet
                var json = JsonConvert.SerializeObject(packet);
                await packetStream.WriteString(json);

                // Write data to websocket
                await WebSocket.SendAsync(
                    memoryStream.ToArray(),
                    WebSocketMessageType.Binary,
                    WebSocketMessageFlags.None,
                    CancellationToken.Token
                );

                // Cleanup
                await memoryStream.DisposeAsync();
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"Error in read loop: {e.Message}");
            }
        }

        await Close();
    }

    private async Task ProcessLoop()
    {
        while (!CancellationToken.IsCancellationRequested)
        {
            // Select packet if any
            object? packet = null;

            lock (ProcessQueue)
            {
                if (ProcessQueue.Count > 0)
                    packet = ProcessQueue.Dequeue();
            }

            if (packet == null)
            {
                // No packet found, wait a little bit and try again
                await Task.Delay(10);
                continue;
            }

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                OnPacket?.Invoke(null, packet);
            }
            catch (Exception e)
            {
                OnLogError?.Invoke($"Packet handler for '{packet.GetType().FullName}' threw an exception: {e.Message}");
            }

            sw.Stop();

            if (sw.Elapsed.Seconds > 3)
            {
                OnLogError?.Invoke(
                    $"Packet handler for '{packet.GetType().FullName}' took to long ({sw.Elapsed.Seconds}");
            }
        }
    }

    #endregion

    public Task Start()
    {
        Task.Run(ReadLoop);
        Task.Run(WriteLoop);
        Task.Run(ProcessLoop);
        
        return Task.CompletedTask;
    }

    public Task Send(object packet)
    {
        lock (WriteQueue)
            WriteQueue.Enqueue(packet);
        
        return Task.CompletedTask;
    }

    public async Task Close()
    {
        OnDisconnected?.Invoke(null, null!);
        CancellationToken.Cancel();

        if (WebSocket.State == WebSocketState.Open)
        {
            await WebSocket.CloseAsync(
                WebSocketCloseStatus.Empty,
                null,
                System.Threading.CancellationToken.None
            );
        }

        TaskCompletion.SetResult();
    }
    
    // Utils
    public int GetWriteQueueLenght()
    {
        lock (WriteQueue)
            return WriteQueue.Count;
    }

    public Task WaitForClose()
    {
        return TaskCompletion.Task;
    }
}
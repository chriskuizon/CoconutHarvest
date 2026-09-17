using System.Text.Json;
using CoconutHarvest.Contracts;
using NetMQ;
using NetMQ.Sockets;

namespace CoconutHarvest.Perception;

/// <summary>ZeroMQ PUB socket. Mission logic subscribes on the same endpoint.</summary>
public sealed class DetectionPublisher : IDisposable
{
    private readonly PublisherSocket _socket;

    public DetectionPublisher(string endpoint)
    {
        _socket = new PublisherSocket();
        _socket.Options.SendHighWatermark = 5; // drop old frames rather than queue them
        _socket.Bind(endpoint);
    }

    public void Publish(DetectionFrame frame)
    {
        var json = JsonSerializer.Serialize(frame, ContractsJsonContext.Default.DetectionFrame);
        _socket.SendMoreFrame(DetectionFrame.Topic).SendFrame(json);
    }

    public void Dispose()
    {
        _socket.Dispose();
        NetMQConfig.Cleanup(false);
    }
}

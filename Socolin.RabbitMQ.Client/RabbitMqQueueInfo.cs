namespace Socolin.RabbitMQ.Client;

public struct RabbitMqQueueInfo
{
    public long MessageCount { get; set; }
    public int ConsumerCount { get; set; }
}
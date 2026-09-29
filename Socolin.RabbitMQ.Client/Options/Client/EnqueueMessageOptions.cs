namespace Socolin.RabbitMQ.Client.Options.Client;

public sealed record EnqueueMessageOptions
{
	public byte? Priority { get; init; }
}

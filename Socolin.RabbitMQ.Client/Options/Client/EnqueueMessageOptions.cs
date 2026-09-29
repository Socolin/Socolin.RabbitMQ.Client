using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options.Client;

public sealed record EnqueueMessageOptions
{
	internal static readonly EnqueueMessageOptions Default = new();

	public Dictionary<string, object>? ContextItems { get; init; }
	public byte? Priority { get; init; }
}

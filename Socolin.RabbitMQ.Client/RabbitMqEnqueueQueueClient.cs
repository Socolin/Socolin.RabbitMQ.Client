using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client;

[PublicAPI]
public interface IRabbitMqEnqueueQueueClient
{
	Task EnqueueMessageAsync(object message, Dictionary<string, object>? contextItems = null, EnqueueMessageOptions? options = null);
	Task EnqueueMessageAsync(object message, string contentType, EnqueueMessageOptions? options = null);
}

public class RabbitMqEnqueueQueueClient : IRabbitMqEnqueueQueueClient
{
	private readonly ReadOnlyMemory<IClientPipe> _pipeline;

	public RabbitMqEnqueueQueueClient(ReadOnlyMemory<IClientPipe> pipeline)
	{
		_pipeline = pipeline;
	}

	public async Task EnqueueMessageAsync(
		object message,
		Dictionary<string, object>? contextItems = null,
		EnqueueMessageOptions? options = null
	)
	{
		var pipeMessage = new ClientPipeContextMessage(message, contextItems);
		pipeMessage.SetPriority(options?.Priority);
		await ClientPipe.ExecutePipelineAsync(pipeMessage, _pipeline);
	}

	public Task EnqueueMessageAsync(object message, string contentType, EnqueueMessageOptions? options = null)
	{
		return EnqueueMessageAsync(message, new Dictionary<string, object>
		{
			[SerializerClientPipe.ContentTypeKeyName] = contentType
		}, options);
	}
}

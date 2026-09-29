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
	Task EnqueueMessageAsync(object message, Dictionary<string, object>? contextItems = null);
	Task EnqueueMessageAsync(object message, EnqueueMessageOptions options);
	Task EnqueueMessageAsync(object message, string contentType, EnqueueMessageOptions? options = null);
}

public class RabbitMqEnqueueQueueClient(
	ReadOnlyMemory<IClientPipe> pipeline
) : IRabbitMqEnqueueQueueClient
{
	public async Task EnqueueMessageAsync(
		object message,
		Dictionary<string, object>? contextItems = null
	)
	{
		var pipeMessage = new ClientPipeContextMessage(message, contextItems);
		await ClientPipe.ExecutePipelineAsync(pipeMessage, pipeline);
	}

	public async Task EnqueueMessageAsync(
		object message,
		EnqueueMessageOptions options
	)
	{
		var pipeMessage = new ClientPipeContextMessage(message, options.ContextItems);
		pipeMessage.SetPriority(options.Priority);
		await ClientPipe.ExecutePipelineAsync(pipeMessage, pipeline);
	}

	public Task EnqueueMessageAsync(object message, string contentType, EnqueueMessageOptions? options = null)
	{
		return EnqueueMessageAsync(message,
			(options ?? EnqueueMessageOptions.Default) with
			{
				ContextItems = new Dictionary<string, object>(options?.ContextItems ?? [])
				{
					[SerializerClientPipe.ContentTypeKeyName] = contentType
				},
			}
		);
	}
}

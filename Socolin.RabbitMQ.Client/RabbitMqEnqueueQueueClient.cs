using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes;
using Socolin.RabbitMQ.Client.Pipes.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client
{
	public interface IRabbitMqEnqueueQueueClient
	{
		Task EnqueueMessageAsync(object message);
	}

	public class RabbitMqEnqueueQueueClient : IRabbitMqEnqueueQueueClient
	{
		private readonly ReadOnlyMemory<IClientPipe> _pipeline;

		public RabbitMqEnqueueQueueClient(ReadOnlyMemory<IClientPipe> pipeline)
		{
			_pipeline = pipeline;
		}

		public async Task EnqueueMessageAsync(object message)
		{
			var pipeMessage = new ClientPipeContextMessage(message);
			await ClientPipe.ExecutePipelineAsync(pipeMessage, _pipeline);
		}
	}
}
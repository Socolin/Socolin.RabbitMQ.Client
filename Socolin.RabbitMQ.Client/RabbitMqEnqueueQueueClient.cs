using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client
{
	public interface IRabbitMqEnqueueQueueClient
	{
		Task EnqueueMessageAsync(object message);
	}

	public class RabbitMqEnqueueQueueClient : IRabbitMqEnqueueQueueClient
	{
		private readonly ReadOnlyMemory<IPipe> _pipeline;

		public RabbitMqEnqueueQueueClient(ReadOnlyMemory<IPipe> pipeline)
		{
			_pipeline = pipeline;
		}

		public async Task EnqueueMessageAsync(object message)
		{
			var pipeMessage = new PipeContextMessage(message);
			await Pipe.ExecutePipelineAsync(pipeMessage, _pipeline);
		}
	}
}
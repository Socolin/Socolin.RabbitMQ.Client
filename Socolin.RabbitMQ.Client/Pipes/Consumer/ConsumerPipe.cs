using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public interface IConsumerPipe<T> where T : class
	{
		Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline);
	}

	public abstract class ConsumerPipe<T> : IConsumerPipe<T> where T : class
	{
		protected Task ProcessNextAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			if (context.ActiveMessageProcessorCanceller.IsInterrupted())
				return Task.CompletedTask;
			return ExecutePipelineAsync(context, pipeline);
		}

		public static Task ExecutePipelineAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			return pipeline.Span[0].ProcessAsync(context, pipeline.Slice(1));
		}

		public abstract Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline);
	}
}
using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.ConsumerPipes.Context;

namespace Socolin.RabbitMQ.Client.ConsumerPipes
{
	public interface IConsumerPipe<T> where T : class
	{
		Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline);
	}

	public abstract class ConsumerPipe<T> : IConsumerPipe<T> where T : class
	{
		protected Task ProcessNextAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			return ExecutePipelineAsync(context, pipeline);
		}

		public static Task ExecutePipelineAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			return pipeline.Span[0].ProcessAsync(context, pipeline.Slice(1));
		}

		public abstract Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline);
	}
}
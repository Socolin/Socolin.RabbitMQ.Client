using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class MessageProcessorPipe<T> : IConsumerPipe<T> where T : class
	{
		public Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			return context.MessageProcessor.Invoke(context.DeserializedMessage!, context.Items);
		}
	}
}
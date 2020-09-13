using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class SimpleMessageAcknowledgementPipe<T> : ConsumerPipe<T> where T : class
	{
		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			try
			{
				await ProcessNextAsync(context, pipeline);
				context.Chanel.BasicAck(context.RabbitMqMessage.DeliveryTag, false);
			}
			catch (Exception)
			{
				context.Chanel.BasicReject(context.RabbitMqMessage.DeliveryTag, false);
			}
		}
	}
}
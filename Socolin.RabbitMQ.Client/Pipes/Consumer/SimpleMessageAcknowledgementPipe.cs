using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class SimpleMessageAcknowledgementPipe<T> : ConsumerPipe<T> where T : class
	{
		private const string FinalAttemptItemsKey = "FinalAttempt";

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
		{
			try
			{
				await ProcessNextAsync(context, pipeline, cancellationToken);
				context.Items[FinalAttemptItemsKey] = true;
				await context.Chanel.BasicAckAsync(context.RabbitMqMessage.DeliveryTag, false, cancellationToken);
			}
			catch (Exception)
			{
				await context.Chanel.BasicRejectAsync(context.RabbitMqMessage.DeliveryTag, false, cancellationToken);
			}
		}
	}
}
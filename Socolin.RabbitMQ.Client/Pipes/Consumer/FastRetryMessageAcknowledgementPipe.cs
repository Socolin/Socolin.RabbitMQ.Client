using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class FastRetryMessageAcknowledgementPipe<T> : ConsumerPipe<T> where T : class
	{
		private const string DefaultRetryCountHeader = "RetryCount";
		private const string FinalAttemptItemsKey = "FinalAttempt";

		private readonly int _maximumRetryCount;
		private readonly string _retryCountHeaderName;

		public FastRetryMessageAcknowledgementPipe(int maximumRetryCount, string? retryCountHeaderName = null)
		{
			_maximumRetryCount = maximumRetryCount;
			_retryCountHeaderName = retryCountHeaderName ?? DefaultRetryCountHeader;
		}

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			var rMessage = context.RabbitMqMessage;

			AddFinalAttemptHeader(context);

			try
			{
				await ProcessNextAsync(context, pipeline);
				if (context.ActiveMessageProcessorCanceller.IsInterrupted())
					return;

				context.Chanel.BasicAck(rMessage.DeliveryTag, false);
			}
			catch (Exception)
			{
				if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				{
					rMessage.BasicProperties.Headers ??= new Dictionary<string, object>();
					rMessage.BasicProperties.Headers[_retryCountHeaderName] = 1;
					context.Chanel.BasicPublish(RabbitMqConstants.DefaultExchangeName, rMessage.RoutingKey, true, rMessage.BasicProperties, rMessage.Body);
					context.Chanel.BasicAck(rMessage.DeliveryTag, false);
				}
				else if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < _maximumRetryCount)
				{
					rMessage.BasicProperties.Headers[_retryCountHeaderName] = retryCount + 1;
					context.Chanel.BasicPublish(RabbitMqConstants.DefaultExchangeName, rMessage.RoutingKey, true, rMessage.BasicProperties, rMessage.Body);
					context.Chanel.BasicAck(rMessage.DeliveryTag, false);
				}
				else
				{
					context.Chanel.BasicReject(rMessage.DeliveryTag, false);
				}
			}
		}

		private void AddFinalAttemptHeader(IConsumerPipeContext<T> context)
		{
			var rMessage = context.RabbitMqMessage;
			if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				return;
			if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < _maximumRetryCount)
				return;
			context.Items[FinalAttemptItemsKey] = true;
		}
	}
}
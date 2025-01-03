using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class FastRetryMessageAcknowledgementPipe<T>(
		int maximumRetryCount,
		string? retryCountHeaderName = null
	)
		: ConsumerPipe<T>
		where T : class
	{
		private const string DefaultRetryCountHeader = "RetryCount";
		private const string FinalAttemptItemsKey = "FinalAttempt";

		private readonly string _retryCountHeaderName = retryCountHeaderName ?? DefaultRetryCountHeader;

		public override async Task ProcessAsync(
			IConsumerPipeContext<T> context,
			ReadOnlyMemory<IConsumerPipe<T>> pipeline,
			CancellationToken cancellationToken = default
		)
		{
			var rMessage = context.RabbitMqMessage;

			AddFinalAttemptHeader(context);

			try
			{
				await ProcessNextAsync(context, pipeline, cancellationToken);
				if (context.ActiveMessageProcessorCanceller.IsInterrupted())
					return;

				await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
			}
			catch (Exception)
			{
				if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				{
					var basicProperties = new BasicProperties(rMessage.BasicProperties);
					basicProperties.Headers ??= new Dictionary<string, object?>();
					basicProperties.Headers[_retryCountHeaderName] = 1;
					using var channelContainer = await context.ConnectionManager.AcquireChannelAsync(ChannelType.Publish);
					await channelContainer.Channel.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, rMessage.RoutingKey, true, basicProperties, rMessage.Body, cancellationToken);
					await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
				else if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < maximumRetryCount)
				{
					var basicProperties = new BasicProperties(rMessage.BasicProperties);
					basicProperties.Headers ??= new Dictionary<string, object?>();
					basicProperties.Headers[_retryCountHeaderName] = retryCount + 1;
					using var channelContainer = await context.ConnectionManager.AcquireChannelAsync(ChannelType.Publish);
					await channelContainer.Channel.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, rMessage.RoutingKey, true, basicProperties, rMessage.Body, cancellationToken);
					await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
				else
				{
					await context.Chanel.BasicRejectAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
			}
		}

		private void AddFinalAttemptHeader(IConsumerPipeContext<T> context)
		{
			var rMessage = context.RabbitMqMessage;
			if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				return;
			if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < maximumRetryCount)
				return;
			context.Items[FinalAttemptItemsKey] = true;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class DelayedRetryMessageAcknowledgementPipe<T> : ConsumerPipe<T> where T : class
	{
		private const string DefaultDelayedRetryCountHeader = "DelayedRetryCount";
		private const string FinalAttemptItemsKey = "FinalAttempt";

		private readonly TimeSpan[] _retryDelays;
		private readonly string? _delayedMessagesQueueName;
		private readonly string _retryCountHeaderName;

		public DelayedRetryMessageAcknowledgementPipe(TimeSpan[] retryDelays, string? delayedMessagesQueueName = null, string? delayedRetryCountHeaderName = null)
		{
			if (retryDelays.Length < 1)
				throw new ArgumentOutOfRangeException(nameof(retryDelays), "retryDelays must be an array of at least 1 element");
			_retryDelays = retryDelays;
			_delayedMessagesQueueName = delayedMessagesQueueName;
			_retryCountHeaderName = delayedRetryCountHeaderName ?? DefaultDelayedRetryCountHeader;
		}

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
		{
			var rMessage = context.RabbitMqMessage;

			AddFinalAttemptHeader(context);

			try
			{
				await ProcessNextAsync(context, pipeline, cancellationToken);
				await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
			}
			catch (Exception)
			{
				if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				{
					var basicProperties = new BasicProperties(rMessage.BasicProperties);
					basicProperties.Headers ??= new Dictionary<string, object?>();
					basicProperties.Headers[_retryCountHeaderName] = 1;
					basicProperties.Expiration = _retryDelays[0].TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
					using var channelContainer = await context.ConnectionManager.AcquireChannelAsync(ChannelType.Publish);
					var delayedMessagesQueueName = _delayedMessagesQueueName ?? DelayedQueueName(rMessage.RoutingKey);
					await channelContainer.Channel.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, delayedMessagesQueueName, true, basicProperties, rMessage.Body, cancellationToken);
					await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
				else if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < _retryDelays.Length)
				{
					var basicProperties = new BasicProperties(rMessage.BasicProperties);
					basicProperties.Headers ??= new Dictionary<string, object?>();
					basicProperties.Headers[_retryCountHeaderName] = retryCount + 1;
					basicProperties.Expiration = _retryDelays[retryCount].TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
					using var channelContainer = await context.ConnectionManager.AcquireChannelAsync(ChannelType.Publish);
					var delayedMessagesQueueName = _delayedMessagesQueueName ?? DelayedQueueName(rMessage.RoutingKey);
					await channelContainer.Channel.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, delayedMessagesQueueName, true, basicProperties, rMessage.Body, cancellationToken);
					await context.Chanel.BasicAckAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
				else
				{
					await context.Chanel.BasicRejectAsync(rMessage.DeliveryTag, false, cancellationToken);
				}
			}
		}

		public static async Task<string> CreateDelayQueueAsync(IRabbitMqServiceClient serviceClient, string baseQueueName)
		{
			string delayedQueueName = DelayedQueueName(baseQueueName);

			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.Durable()
				.WithDeadLetterExchange(RabbitMqConstants.DefaultExchangeName)
				.WithDeadLetterRoutingKey(baseQueueName)
				.Build();

			await serviceClient.CreateQueueAsync(delayedQueueName, options);

			return delayedQueueName;
		}

		private void AddFinalAttemptHeader(IConsumerPipeContext<T> context)
		{
			var rMessage = context.RabbitMqMessage;
			if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				return;
			if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < _retryDelays.Length)
				return;
			context.Items[FinalAttemptItemsKey] = true;
		}

		private static string DelayedQueueName(string baseQueueName)
		{
			return baseQueueName + "-Delayed";
		}
	}
}
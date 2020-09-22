using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			var rMessage = context.RabbitMqMessage;

			AddFinalAttemptHeader(context);

			try
			{
				await ProcessNextAsync(context, pipeline);
				context.Chanel.BasicAck(rMessage.DeliveryTag, false);
			}
			catch (Exception)
			{
				if (rMessage.BasicProperties.Headers?.ContainsKey(_retryCountHeaderName) != true)
				{
					rMessage.BasicProperties.Headers ??= new Dictionary<string, object>();
					rMessage.BasicProperties.Headers[_retryCountHeaderName] = 1;
					rMessage.BasicProperties.Expiration = _retryDelays[0].TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
					context.Chanel.BasicPublish(RabbitMqConstants.DefaultExchangeName, _delayedMessagesQueueName ?? DelayedQueueName(rMessage.RoutingKey), true, rMessage.BasicProperties, rMessage.Body);
					context.Chanel.BasicAck(rMessage.DeliveryTag, false);
				}
				else if (rMessage.BasicProperties.Headers?[_retryCountHeaderName] is int retryCount && retryCount < _retryDelays.Length)
				{
					rMessage.BasicProperties.Headers[_retryCountHeaderName] = retryCount + 1;
					rMessage.BasicProperties.Expiration = _retryDelays[retryCount].TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
					context.Chanel.BasicPublish(RabbitMqConstants.DefaultExchangeName, _delayedMessagesQueueName ?? DelayedQueueName(rMessage.RoutingKey), true, rMessage.BasicProperties, rMessage.Body);
					context.Chanel.BasicAck(rMessage.DeliveryTag, false);
				}
				else
				{
					context.Chanel.BasicReject(rMessage.DeliveryTag, false);
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
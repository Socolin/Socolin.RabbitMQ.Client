using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Builders;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Options.Consumer
{
	public class ConsumerOptionsBuilder<T> where T : class
	{
		private readonly IDictionary<string, Func<ReadOnlyMemory<byte>, T>> _deserializers = new Dictionary<string, Func<ReadOnlyMemory<byte>, T>>();
		private Func<ReadOnlyMemory<byte>, T>? _defaultDeserializer;
		private readonly List<IConsumerPipeBuilder<T>> _customPipes = new List<IConsumerPipeBuilder<T>>();
		private IConsumerPipeBuilder<T>? _messageAcknowledgmentPipeBuilder;

		public ConsumerOptionsBuilder<T> WithDefaultDeSerializer(Func<ReadOnlyMemory<byte>, T> deserializer)
		{
			_defaultDeserializer = deserializer;
			return this;
		}

		/// <summary>
		/// Configure the deserializer to use for a given ContentType. If a message's ContentType does not match
		/// any deserializer, it use the default one
		/// </summary>
		/// <param name="deserializer"></param>
		/// <param name="contentType"></param>
		/// <returns></returns>
		public ConsumerOptionsBuilder<T> WithDeSerializer(Func<ReadOnlyMemory<byte>, T> deserializer, string contentType)
		{
			_deserializers[contentType] = deserializer;
			return this;
		}

		public ConsumerOptionsBuilder<T> WithCustomPipe(IConsumerPipeBuilder<T> pipeBuilder)
		{
			_customPipes.Add(pipeBuilder);
			return this;
		}

		/// <summary>
		/// Configure the pipeline to Ack successfully processed messages (no exception has been thrown when executing
		/// <see cref="IConsumerPipeContext{T}.MessageProcessor">MessageProcessor</see>) and to reject message on error.
		/// </summary>
		public ConsumerOptionsBuilder<T> WithSimpleMessageAck()
		{
			_messageAcknowledgmentPipeBuilder = new DefaultConsumerPipeBuilder<T>(() =>
				new SimpleMessageAcknowledgementPipe<T>()
			);
			return this;
		}

		/// <summary>
		/// Configure the pipeline to Ack successfully processed messages (no exception has been thrown when executing
		/// <see cref="IConsumerPipeContext{T}.MessageProcessor">MessageProcessor</see>) and to retry message when
		/// processing fail. On failure the message is immediately requeue.
		/// If the message has been retried too many time, it will be rejected.
		/// </summary>
		/// <param name="maxRetryCount"></param>
		/// <param name="retryCountHeaderName">The name of the header used to keep track of the number of retry. Default="RetryCount"</param>
		public ConsumerOptionsBuilder<T> WithFastRetryMessageAck(int maxRetryCount, string? retryCountHeaderName = null)
		{
			_messageAcknowledgmentPipeBuilder = new DefaultConsumerPipeBuilder<T>(() =>
				new FastRetryMessageAcknowledgementPipe<T>(maxRetryCount, retryCountHeaderName)
			);
			return this;
		}

		/// <summary>
		/// Configure the pipeline to Ack successfully processed messages (no exception has been thrown when executing
		/// <see cref="IConsumerPipeContext{T}.MessageProcessor">MessageProcessor</see>) and to retry message when
		/// processing fail. On failure the message is requeue with an expiration time in another queue that *must*
		/// be created with the original queue as dead letter queue. This can be done by calling
		/// <see cref="DelayedRetryMessageAcknowledgementPipe{T}.CreateDelayQueueAsync"/>
		/// If the message has been retried too many time, it will be rejected.
		/// </summary>
		/// <param name="retryDelays"></param>
		/// <param name="delayedMessagesQueueName">The name of the queue used to keep message until they expire and
		/// they are re-queued to the original queue. Default : queueName + "-Delayed"</param>
		/// <param name="retryCountHeaderName">The name of the header used to keep track of the number of retry.
		/// Default : "DelayedRetryCount"</param>
		public ConsumerOptionsBuilder<T> WithDelayedRetryMessageAck(TimeSpan[] retryDelays, string? delayedMessagesQueueName = null, string? retryCountHeaderName = null)
		{
			_messageAcknowledgmentPipeBuilder = new DefaultConsumerPipeBuilder<T>(() =>
				new DelayedRetryMessageAcknowledgementPipe<T>(retryDelays, delayedMessagesQueueName, retryCountHeaderName)
			);
			return this;
		}

		/// <summary>
		/// Configure the pipe responsible to Ack/Reject the messages
		/// </summary>
		/// <param name="pipeBuilder"></param>
		/// <returns></returns>
		public ConsumerOptionsBuilder<T> WitheMessageAck(IConsumerPipeBuilder<T> pipeBuilder)
		{
			_messageAcknowledgmentPipeBuilder = pipeBuilder;
			return this;
		}

		public ConsumerOptions<T> Build()
		{
			if (_defaultDeserializer == null)
				throw new InvalidRabbitMqOptionException("Please provide a deserializer in options before listening to a queue");

			var options = new ConsumerOptions<T>(new DeserializationPipeOptions<T>(_defaultDeserializer, new ReadOnlyDictionary<string, Func<ReadOnlyMemory<byte>, T>>(_deserializers)))
			{
				MessageAcknowledgmentPipeBuilder = _messageAcknowledgmentPipeBuilder
			};

			options.Customs.AddRange(_customPipes);

			return options;
		}
	}
}
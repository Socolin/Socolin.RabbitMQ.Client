using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Socolin.RabbitMQ.Client.ConsumerPipes;
using Socolin.RabbitMQ.Client.ConsumerPipes.Builders;

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

		public ConsumerOptionsBuilder<T> WithSimpleMessageAck()
		{
			_messageAcknowledgmentPipeBuilder = new DefaultConsumerPipeBuilder<T>(() =>
				new SimpleMessageAcknowledgementPipe<T>()
			);
			return this;
		}

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
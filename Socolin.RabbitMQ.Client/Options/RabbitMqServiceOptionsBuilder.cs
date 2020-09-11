using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Builders;

namespace Socolin.RabbitMQ.Client.Options
{
	public class RabbitMqServiceOptionsBuilder
	{
		private IRabbitMqConnectionManager? _connectionManager;

		private int? _maxRetryCount;
		private TimeSpan? _maxRetryDuration;
		private TimeSpan? _delayBetweenRetry;
		private IGenericPipeBuilder? _retryPipeBuilder;

		private Func<object, byte[]>? _serializer;
		private string? _contentType;

		private readonly IDictionary<string, Func<Type, ReadOnlyMemory<byte>, object>> _deserializers = new Dictionary<string, Func<Type, ReadOnlyMemory<byte>, object>>();
		private Func<Type, ReadOnlyMemory<byte>, object>? _defaultDeserializer;

		private readonly IList<IPipeBuilder> _customPipeBuilders = new List<IPipeBuilder>();

		public RabbitMqServiceOptionsBuilder WithConnectionManager(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithRetry(IGenericPipeBuilder retryPipeBuilder)
		{
			if (_delayBetweenRetry != null)
				throw new InvalidBuilderOptionsException("You need to choose either to use built-in retry logic or a custom pipe, but not both");
			_retryPipeBuilder = retryPipeBuilder;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithRetry(TimeSpan? maxRetryDuration, int? maxRetryCount, TimeSpan delayBetweenRetry)
		{
			if (_retryPipeBuilder != null)
				throw new InvalidBuilderOptionsException("You need to choose either to use built-in retry logic or a custom pipe, but not both");
			_maxRetryCount = maxRetryCount;
			_maxRetryDuration = maxRetryDuration;
			_delayBetweenRetry = delayBetweenRetry;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithSerializer(Func<object, byte[]> serializer, string? contentType)
		{
			_serializer = serializer;
			_contentType = contentType;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithDefaultDeSerializer(Func<Type, ReadOnlyMemory<byte>, object> deserializer)
		{
			_defaultDeserializer = deserializer;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithDeSerializer(Func<Type, ReadOnlyMemory<byte>, object> deserializer, string contentType)
		{
			_deserializers[contentType] = deserializer;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IActionPipeBuilder pipeBuilder)
		{
			_customPipeBuilders.Add(pipeBuilder);
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IMessagePipeBuilder pipeBuilder)
		{
			_customPipeBuilders.Add(pipeBuilder);
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IGenericPipeBuilder pipeBuilder)
		{
			_customPipeBuilders.Add(pipeBuilder);
			return this;
		}

		public RabbitMqServiceClientOptions Build()
		{
			if (_connectionManager == null)
				throw new InvalidBuilderOptionsException("Missing connectionManager call .WithConnectionManager()");

			var options = new RabbitMqServiceClientOptions(
				_connectionManager
			);

			if (_serializer != null)
				options.Serialization = new SerializationOption(_serializer, _contentType);
			if (_defaultDeserializer != null)
				options.Deserialization = new DeserializationGenericPipeOption(_defaultDeserializer, new ReadOnlyDictionary<string, Func<Type, ReadOnlyMemory<byte>, object>>(_deserializers));

			if (_retryPipeBuilder != null)
				options.Retry = _retryPipeBuilder;
			else if (_delayBetweenRetry != null)
				options.Retry = new RetryPipeBuilder(_maxRetryDuration, _maxRetryCount, _delayBetweenRetry.Value);

			options.CustomPipes.AddRange(_customPipeBuilders);

			return options;
		}
	}
}
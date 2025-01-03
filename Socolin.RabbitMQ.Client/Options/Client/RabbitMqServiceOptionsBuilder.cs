using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Client.Builders;

namespace Socolin.RabbitMQ.Client.Options.Client
{
	public class RabbitMqServiceOptionsBuilder
	{
		private IRabbitMqConnectionManager? _connectionManager;

		private int? _maxRetryCount;
		private TimeSpan? _maxRetryDuration;
		private TimeSpan? _delayBetweenRetry;
		private IGenericClientPipeBuilder? _retryPipeBuilder;

		private SerializerDelegate? _serializer;
		private string? _contentType;

		private readonly Dictionary<string, SerializerDelegate> _serializers = new Dictionary<string, SerializerDelegate>();

		private readonly IList<IClientPipeBuilder> _customPipeBuilders = new List<IClientPipeBuilder>();
		private bool _usePerMessageTtl;
		private int? _perMessageTTl;
		private TimeSpan[]? _delaysBetweenRetry;
		private DeliveryModes? _deliveryMode;

		public RabbitMqServiceOptionsBuilder WithConnectionManager(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithRetry(IGenericClientPipeBuilder retryClientPipeBuilder)
		{
			if (_delayBetweenRetry != null)
				throw new InvalidBuilderOptionsException("You need to choose either to use built-in retry logic or a custom pipe, but not both");
			_retryPipeBuilder = retryClientPipeBuilder;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithRetry(TimeSpan[] delaysBetweenRetry)
		{
			if (_retryPipeBuilder != null)
				throw new InvalidBuilderOptionsException("You need to choose either to use built-in retry logic or a custom pipe, but not both");
			_delaysBetweenRetry = delaysBetweenRetry;
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

		public RabbitMqServiceOptionsBuilder WithDefaultSerializer(SerializerDelegate serializer, string? contentType)
		{
			_serializer = serializer;
			_contentType = contentType;

			return this;
		}

		public RabbitMqServiceOptionsBuilder WithSerializer(SerializerDelegate serializer, string? contentType)
		{
			if (contentType != null)
				_serializers[contentType] = serializer;

			if (_serializer == null)
			{
				_serializer = serializer;
				_contentType = contentType;
			}

			return this;
		}

		public RabbitMqServiceOptionsBuilder WithPerMessageTtl(int? ttlMilliseconds = null)
		{
			if (ttlMilliseconds == null)
			{
				_usePerMessageTtl = true;
				return this;
			}

			if (ttlMilliseconds < 0)
				throw new ArgumentOutOfRangeException(nameof(ttlMilliseconds));
			_perMessageTTl = ttlMilliseconds;
			_usePerMessageTtl = true;
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IActionClientPipeBuilder clientPipeBuilder)
		{
			_customPipeBuilders.Add(clientPipeBuilder);
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IMessageClientPipeBuilder clientPipeBuilder)
		{
			_customPipeBuilders.Add(clientPipeBuilder);
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithCustomPipe(IGenericClientPipeBuilder clientPipeBuilder)
		{
			_customPipeBuilders.Add(clientPipeBuilder);
			return this;
		}

		public RabbitMqServiceOptionsBuilder WithDeliveryMode(DeliveryModes deliveryMode)
		{
			_deliveryMode = deliveryMode;
			return this;
		}

		public RabbitMqServiceClientOptions Build()
		{
			if (_connectionManager == null)
				throw new InvalidBuilderOptionsException("Missing connectionManager call .WithConnectionManager()");

			var options = new RabbitMqServiceClientOptions(
				_connectionManager
			);

			if (_serializer != null || _serializers.Count > 0)
				options.Serialization = new SerializationOptions(_serializer, _contentType, _serializers);

			if (_retryPipeBuilder != null)
				options.Retry = _retryPipeBuilder;
			else if (_delayBetweenRetry != null)
				options.Retry = new RetryClientPipeBuilder(_maxRetryDuration, _maxRetryCount, _delayBetweenRetry.Value);
			else if (_delaysBetweenRetry != null)
				options.Retry = new RetryClientPipeBuilder(_delaysBetweenRetry);

			if (_usePerMessageTtl)
				options.PerMessageTtl = new PerMessageTtlOption(_perMessageTTl);
			options.CustomPipes.AddRange(_customPipeBuilders);
			options.DeliveryMode = _deliveryMode;

			return options;
		}
	}
}
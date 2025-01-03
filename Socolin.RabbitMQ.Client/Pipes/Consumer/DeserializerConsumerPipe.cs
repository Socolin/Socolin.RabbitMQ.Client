using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Options.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class DeserializerConsumerPipe<T> : ConsumerPipe<T> where T : class
	{
		private readonly DeserializationPipeOptions<T> _options;

		public DeserializerConsumerPipe(DeserializationPipeOptions<T> options)
		{
			_options = options;
		}

		public override Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
		{
			var message = context.RabbitMqMessage;

			if (message.BasicProperties.ContentType != null && _options.Deserializers.TryGetValue(message.BasicProperties.ContentType, out var deserializer))
				context.DeserializedMessage = deserializer(message.Body);
			else if (_options.DefaultDeserializer != null)
				context.DeserializedMessage = _options.DefaultDeserializer(message.Body);
			else
				throw new NotSupportedException($"No deserializer for {message.BasicProperties.ContentType} nor default serializer was provided");

			return ProcessNextAsync(context, pipeline, cancellationToken);
		}
	}
}
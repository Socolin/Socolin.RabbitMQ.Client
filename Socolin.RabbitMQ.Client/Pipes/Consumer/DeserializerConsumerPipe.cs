using System;
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

		public override Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			var message = context.RabbitMqMessage;

			if (message.BasicProperties.ContentType != null && _options.Deserializers.ContainsKey(message.BasicProperties.ContentType))
				context.DeserializedMessage = _options.Deserializers[message.BasicProperties.ContentType](message.Body);
			else if (_options.DefaultDeserializer != null)
				context.DeserializedMessage = _options.DefaultDeserializer(message.Body);
			else
				throw new NotSupportedException($"No deserializer for {message.BasicProperties.ContentType} nor default serializer was provided");

			return ProcessNextAsync(context, pipeline);
		}
	}
}
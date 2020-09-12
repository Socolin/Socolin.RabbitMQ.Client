using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.ConsumerPipes.Context;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.ConsumerPipes
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

			if (_options.Deserializers.ContainsKey(message.BasicProperties.ContentType))
				context.DeserializedMessage = _options.Deserializers[message.BasicProperties.ContentType](message.Body);
			else
				context.DeserializedMessage = _options.DefaultDeserializer(message.Body);

			return ProcessNextAsync(context, pipeline);
		}
	}
}
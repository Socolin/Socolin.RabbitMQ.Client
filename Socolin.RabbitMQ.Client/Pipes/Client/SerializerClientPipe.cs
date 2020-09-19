using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class SerializerClientPipe : ClientPipe, IMessageClientPipe
	{
		public const string ContentTypeKeyName = "Content-Type";
		private readonly SerializationOptions _options;

		public SerializerClientPipe(SerializationOptions options)
		{
			_options = options;
		}

		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.BasicProperties = clientPipeContextMessage.Channel!.CreateBasicProperties();
			if (clientPipeContextMessage.TryGetOptionalItemValue<string>(ContentTypeKeyName, out var messageContentType))
			{
				if (!_options.Serializers.TryGetValue(messageContentType, out var serializer))
					throw new SerializerNotFoundException($"Missing serializer for Content-Type: '{messageContentType}'");

				clientPipeContextMessage.BasicProperties.ContentType = messageContentType;
				clientPipeContextMessage.SerializedMessage = serializer.Invoke(clientPipeContextMessage.Message);
			}
			else if (_options.Serializer != null)
			{
				clientPipeContextMessage.BasicProperties.ContentType = _options.ContentType;
				clientPipeContextMessage.SerializedMessage = _options.Serializer.Invoke(clientPipeContextMessage.Message);
			}
			else
				throw new SerializerNotFoundException($"No default serializer defined. You need to define a default serializer if you don't specify message Content-Type");

			return ProcessNextAsync(clientPipeContextMessage, pipeline);
		}
	}
}
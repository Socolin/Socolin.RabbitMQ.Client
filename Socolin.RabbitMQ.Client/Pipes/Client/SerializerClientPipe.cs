using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class SerializerClientPipe : ClientPipe, IMessageClientPipe
	{
		private readonly Func<object, byte[]> _serializer;
		private readonly string? _contentType;

		public SerializerClientPipe(Func<object, byte[]> serializer, string? contentType)
		{
			_serializer = serializer;
			_contentType = contentType;
		}

		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.BasicProperties = clientPipeContextMessage.Channel!.CreateBasicProperties();
			clientPipeContextMessage.BasicProperties.ContentType = _contentType;
			clientPipeContextMessage.SerializedMessage = _serializer.Invoke(clientPipeContextMessage.Message);
			return ProcessNextAsync(clientPipeContextMessage, pipeline);
		}
	}
}
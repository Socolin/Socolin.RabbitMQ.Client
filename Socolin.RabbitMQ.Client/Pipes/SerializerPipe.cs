using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class SerializerPipe : Pipe, IMessagePipe
	{
		private readonly Func<object, byte[]> _serializer;
		private readonly string? _contentType;

		public SerializerPipe(Func<object, byte[]> serializer, string? contentType)
		{
			_serializer = serializer;
			_contentType = contentType;
		}

		public Task ProcessAsync(PipeContextMessage pipeContextMessage, ReadOnlyMemory<IPipe> pipeline)
		{
			pipeContextMessage.BasicProperties = pipeContextMessage.Channel!.CreateBasicProperties();
			pipeContextMessage.BasicProperties.ContentType = _contentType;
			pipeContextMessage.SerializedMessage = _serializer.Invoke(pipeContextMessage.Message);
			return ProcessNextAsync(pipeContextMessage, pipeline);
		}
	}
}
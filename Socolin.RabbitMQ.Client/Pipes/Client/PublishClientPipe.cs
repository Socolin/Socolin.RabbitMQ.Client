using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PublishClientPipe : ClientPipe, IMessageClientPipe
	{
		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.Channel!.BasicPublish("", clientPipeContextMessage.QueueName, true, clientPipeContextMessage.BasicProperties, clientPipeContextMessage.SerializedMessage);
			return Task.CompletedTask;
		}
	}
}
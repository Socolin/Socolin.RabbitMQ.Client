using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

// ReSharper disable ArgumentsStyleLiteral

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PublishClientPipe : ClientPipe, IMessageClientPipe
	{
		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.Channel!.BasicPublish(
				clientPipeContextMessage.ExchangeName,
				clientPipeContextMessage.RoutingKey,
				mandatory: true,
				clientPipeContextMessage.BasicProperties,
				clientPipeContextMessage.SerializedMessage
			);
			return Task.CompletedTask;
		}
	}
}
using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

// ReSharper disable ArgumentsStyleLiteral

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PublishClientPipe : ClientPipe, IMessageClientPipe
	{
		private readonly DeliveryMode? _deliveryMode;

		public PublishClientPipe(DeliveryMode? deliveryMode)
		{
			_deliveryMode = deliveryMode;
		}

		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			if (_deliveryMode.HasValue)
				clientPipeContextMessage.BasicProperties!.DeliveryMode = (byte) _deliveryMode.Value;
			clientPipeContextMessage.Channel!.BasicPublish(
				clientPipeContextMessage.ExchangeName,
				clientPipeContextMessage.RoutingKey,
				clientPipeContextMessage.Mandatory,
				clientPipeContextMessage.BasicProperties,
				clientPipeContextMessage.SerializedMessage
			);
			return Task.CompletedTask;
		}
	}
}
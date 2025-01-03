using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

// ReSharper disable ArgumentsStyleLiteral

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PublishClientPipe(DeliveryModes? deliveryMode) : ClientPipe, IMessageClientPipe
	{
		public async Task ProcessAsync(
			ClientPipeContextMessage clientPipeContextMessage,
			ReadOnlyMemory<IClientPipe> pipeline,
			CancellationToken cancellation = default
		)
		{
			if (deliveryMode.HasValue)
				clientPipeContextMessage.BasicProperties.DeliveryMode = deliveryMode.Value;
			await clientPipeContextMessage.Channel!.BasicPublishAsync(
				clientPipeContextMessage.ExchangeName,
				clientPipeContextMessage.RoutingKey,
				clientPipeContextMessage.Mandatory,
				clientPipeContextMessage.BasicProperties,
				clientPipeContextMessage.SerializedMessage,
				cancellation
			);
		}
	}
}
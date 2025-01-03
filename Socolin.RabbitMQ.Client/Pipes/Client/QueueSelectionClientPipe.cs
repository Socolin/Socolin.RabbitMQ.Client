using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client;

public class QueueSelectionClientPipe(string exchangeName, string routingKey) : ClientPipe, IMessageClientPipe
{
	public QueueSelectionClientPipe(string routingKey)
		: this(RabbitMqConstants.DefaultExchangeName, routingKey)
	{
	}

	public Task ProcessAsync(
		ClientPipeContextMessage clientPipeContextMessage,
		ReadOnlyMemory<IClientPipe> pipeline,
		CancellationToken cancellationToken = default
	)
	{
		clientPipeContextMessage.ExchangeName = exchangeName;
		clientPipeContextMessage.RoutingKey = routingKey;
		return ProcessNextAsync(clientPipeContextMessage, pipeline, cancellationToken);
	}
}
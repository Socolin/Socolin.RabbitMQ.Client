using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class QueueSelectionClientPipe : ClientPipe, IMessageClientPipe
	{
		private readonly string _exchangeName;
		private readonly string _routingKey;

		public QueueSelectionClientPipe(string routingKey)
		{
			_exchangeName = RabbitMqConstants.DefaultExchangeName;
			_routingKey = routingKey;
		}

		public QueueSelectionClientPipe(string exchangeName, string routingKey)
		{
			_exchangeName = exchangeName;
			_routingKey = routingKey;
		}

		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.ExchangeName = _exchangeName;
			clientPipeContextMessage.RoutingKey = _routingKey;
			return ProcessNextAsync(clientPipeContextMessage, pipeline);
		}
	}
}
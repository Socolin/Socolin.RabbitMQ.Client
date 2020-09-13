using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class QueueSelectionClientPipe : ClientPipe, IMessageClientPipe
	{
		private readonly string _queueName;

		public QueueSelectionClientPipe(string queueName)
		{
			_queueName = queueName;
		}

		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline)
		{
			clientPipeContextMessage.QueueName = _queueName;
			return ProcessNextAsync(clientPipeContextMessage, pipeline);
		}
	}
}
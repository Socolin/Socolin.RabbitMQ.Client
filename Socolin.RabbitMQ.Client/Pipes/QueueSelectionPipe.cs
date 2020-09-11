using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class QueueSelectionPipe : Pipe, IMessagePipe
	{
		private readonly string _queueName;

		public QueueSelectionPipe(string queueName)
		{
			_queueName = queueName;
		}

		public Task ProcessAsync(PipeContextMessage pipeContextMessage, ReadOnlyMemory<IPipe> pipeline)
		{
			pipeContextMessage.QueueName = _queueName;
			return ProcessNextAsync(pipeContextMessage, pipeline);
		}
	}
}
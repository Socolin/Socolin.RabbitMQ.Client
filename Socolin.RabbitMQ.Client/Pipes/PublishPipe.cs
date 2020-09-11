using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class PublishPipe : Pipe, IMessagePipe
	{
		public Task ProcessAsync(PipeContextMessage pipeContextMessage, ReadOnlyMemory<IPipe> pipeline)
		{
			pipeContextMessage.Channel!.BasicPublish("", pipeContextMessage.QueueName, true, pipeContextMessage.BasicProperties, pipeContextMessage.SerializedMessage);
			return Task.CompletedTask;
		}
	}
}
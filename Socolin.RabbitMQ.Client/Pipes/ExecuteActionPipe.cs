using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class ExecuteActionPipe : Pipe, IActionPipe
	{
		public Task ProcessAsync(PipeContextAction pipeContextAction, ReadOnlyMemory<IPipe> pipeline)
		{
			return pipeContextAction.Action(pipeContextAction.Channel!, pipeContextAction);
		}
	}
}
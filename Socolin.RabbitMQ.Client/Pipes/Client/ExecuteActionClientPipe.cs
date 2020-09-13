using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class ExecuteActionClientPipe : ClientPipe, IActionClientPipe
	{
		public Task ProcessAsync(ClientPipeContextAction clientPipeContextAction, ReadOnlyMemory<IClientPipe> pipeline)
		{
			return clientPipeContextAction.Action(clientPipeContextAction.Channel!, clientPipeContextAction);
		}
	}
}
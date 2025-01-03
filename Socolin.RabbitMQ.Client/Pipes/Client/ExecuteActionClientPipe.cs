using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client;

public class ExecuteActionClientPipe : ClientPipe, IActionClientPipe
{
	public Task ProcessAsync(
		ClientPipeContextAction clientPipeContextAction,
		ReadOnlyMemory<IClientPipe> pipeline,
		CancellationToken cancellation = default
	)
	{
		return clientPipeContextAction.Action(clientPipeContextAction.Channel!, clientPipeContextAction);
	}
}
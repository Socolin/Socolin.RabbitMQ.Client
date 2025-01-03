using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;
using Socolin.RabbitMQ.Client.Pipes.Client.Utils;

namespace Socolin.RabbitMQ.Client.Pipes.Client;

public class RetryClientPipe(IConnectionRetryUtil connectionRetryUtil) : ClientPipe, IGenericClientPipe
{
	public async Task ProcessAsync(
		IClientPipeContext context,
		ReadOnlyMemory<IClientPipe> pipeline,
		CancellationToken cancellationToken = default
	)
	{
		await connectionRetryUtil.ExecuteWithRetryOnErrorsAsync(() => ProcessNextAsync(context, pipeline, cancellationToken));
	}
}
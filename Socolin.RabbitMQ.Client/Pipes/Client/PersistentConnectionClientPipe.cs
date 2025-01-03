using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client;

public class PersistentConnectionClientPipe(
	IRabbitMqConnectionManager connectionManager,
	ChannelType channelType
) : ClientPipe, IGenericClientPipe
{
	public async Task ProcessAsync(
		IClientPipeContext context,
		ReadOnlyMemory<IClientPipe> pipeline,
		CancellationToken cancellationToken = default
	)
	{
		var channelContainer = await connectionManager.AcquireChannelAsync(channelType);
		try
		{
			context.ChannelContainer = channelContainer;
			await ProcessNextAsync(context, pipeline, cancellationToken);
		}
		catch
		{
			// Do not release channel except on error
			context.ChannelContainer = null;
			channelContainer.Dispose();
			throw;
		}
	}
}
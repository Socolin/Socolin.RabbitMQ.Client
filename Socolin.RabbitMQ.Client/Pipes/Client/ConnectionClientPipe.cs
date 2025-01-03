using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client;

public class ConnectionClientPipe(IRabbitMqConnectionManager connectionManager, ChannelType channelType)
	: ClientPipe, IGenericClientPipe
{
	public async Task ProcessAsync(
		IClientPipeContext context,
		ReadOnlyMemory<IClientPipe> pipeline,
		CancellationToken cancellation = default
	)
	{
		using var channelContainer = await connectionManager.AcquireChannelAsync(channelType);
		try
		{
			context.ChannelContainer = channelContainer;
			await ProcessNextAsync(context, pipeline, cancellation);
		}
		finally
		{
			context.ChannelContainer = null;
		}
	}
}
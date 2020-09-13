using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class ConnectionClientPipe : ClientPipe, IGenericClientPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;

		public ConnectionClientPipe(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
		}

		public async Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			using var channelContainer = await _connectionManager.AcquireChannel();
			try
			{
				context.ChannelContainer = channelContainer;
				await ProcessNextAsync(context, pipeline);
			}
			finally
			{
				context.ChannelContainer = null;
			}
		}
	}
}
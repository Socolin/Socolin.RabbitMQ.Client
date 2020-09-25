using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class ConnectionClientPipe : ClientPipe, IGenericClientPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;
		private readonly ChannelType _channelType;

		public ConnectionClientPipe(IRabbitMqConnectionManager connectionManager, ChannelType channelType)
		{
			_connectionManager = connectionManager;
			_channelType = channelType;
		}

		public async Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			using var channelContainer = await _connectionManager.AcquireChannel(_channelType);
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
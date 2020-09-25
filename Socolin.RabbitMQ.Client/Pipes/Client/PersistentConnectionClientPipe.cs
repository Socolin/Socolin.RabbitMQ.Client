using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PersistentConnectionClientPipe : ClientPipe, IGenericClientPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;
		private readonly ChannelType _channelType;

		public PersistentConnectionClientPipe(IRabbitMqConnectionManager connectionManager, ChannelType channelType)
		{
			_connectionManager = connectionManager;
			_channelType = channelType;
		}

		public async Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			var channelContainer = await _connectionManager.AcquireChannel(_channelType);
			try
			{
				context.ChannelContainer = channelContainer;
				await ProcessNextAsync(context, pipeline);
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
}
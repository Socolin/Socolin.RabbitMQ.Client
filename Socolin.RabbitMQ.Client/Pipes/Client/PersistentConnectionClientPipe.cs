using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class PersistentConnectionClientPipe : ClientPipe, IGenericClientPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;

		public PersistentConnectionClientPipe(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
		}

		public async Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			var channelContainer = await _connectionManager.AcquireChannel();
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
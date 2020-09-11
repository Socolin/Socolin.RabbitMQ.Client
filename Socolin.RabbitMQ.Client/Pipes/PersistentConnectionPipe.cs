using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class PersistentConnectionPipe : Pipe, IGenericPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;

		public PersistentConnectionPipe(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
		}

		public async Task ProcessAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline)
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
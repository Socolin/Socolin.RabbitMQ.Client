using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class ConnectionPipe : Pipe, IGenericPipe
	{
		private readonly IRabbitMqConnectionManager _connectionManager;

		public ConnectionPipe(IRabbitMqConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
		}

		public async Task ProcessAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline)
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
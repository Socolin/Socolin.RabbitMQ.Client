using System;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client
{
	public class ChannelContainer : IDisposable
	{
		private readonly IRabbitMqConnectionManager _connectionManager;
		public readonly IModel Channel;

		public ChannelContainer(IRabbitMqConnectionManager connectionManager, IModel channel)
		{
			_connectionManager = connectionManager;
			Channel = channel;
		}

		public void Dispose()
		{
			_connectionManager.ReleaseChannel(Channel);
		}
	}
}
using System;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client
{
	public class ChannelContainer : IDisposable
	{
		private readonly IRabbitMqChannelManager _channelManager;
		public readonly IModel Channel;

		public ChannelContainer(IRabbitMqChannelManager channelManager, IModel channel)
		{
			_channelManager = channelManager;
			Channel = channel;
		}

		public void Dispose()
		{
			_channelManager.ReleaseChannel(Channel);
		}
	}
}
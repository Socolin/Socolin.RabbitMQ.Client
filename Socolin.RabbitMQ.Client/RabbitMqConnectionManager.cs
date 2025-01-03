using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client
{
	public enum ChannelType
	{
		Publish,
		Consumer
	}

	[PublicAPI]
	public interface IRabbitMqConnectionManager : IDisposable
	{
		Task<ChannelContainer> AcquireChannelAsync(ChannelType channelType);
		void ReleaseChannel(ChannelType channelType, IChannel channel);
	}

	public class RabbitMqConnectionManager(Uri uri, string connectionName, TimeSpan connectionTimeout, TimeSpan requestedHeart)
		: IRabbitMqConnectionManager
	{
		private readonly IRabbitMqChannelManager _publishChannelManager = new RabbitMqChannelManager(uri, connectionName, connectionTimeout, ChannelType.Publish, requestedHeart);
		private readonly IRabbitMqChannelManager _consumerChannelManager = new RabbitMqChannelManager(uri, connectionName, connectionTimeout, ChannelType.Consumer, requestedHeart);

		public RabbitMqConnectionManager(Uri uri, string connectionName, TimeSpan connectionTimeout)
			: this(uri, connectionName, connectionTimeout, System.Threading.Timeout.InfiniteTimeSpan)
		{
		}

		public void Dispose()
		{
			_publishChannelManager.Dispose();
			_consumerChannelManager.Dispose();
		}

		public Task<ChannelContainer> AcquireChannelAsync(ChannelType channelType)
		{
			switch (channelType)
			{
				case ChannelType.Publish:
					return _publishChannelManager.AcquireChannelAsync();
				case ChannelType.Consumer:
					return _consumerChannelManager.AcquireChannelAsync();
				default:
					throw new ArgumentOutOfRangeException(nameof(channelType), channelType, null);
			}
		}

		public void ReleaseChannel(ChannelType channelType, IChannel channel)
		{
			switch (channelType)
			{
				case ChannelType.Publish:
					_publishChannelManager.ReleaseChannel(channel);
					break;
				case ChannelType.Consumer:
					_consumerChannelManager.ReleaseChannel(channel);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(channelType), channelType, null);
			}
		}
	}
}
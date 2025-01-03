using System;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client;

public class ChannelContainer(
	IRabbitMqChannelManager channelManager,
	IChannel channel
)
	: IDisposable
{
	public readonly IChannel Channel = channel;

	public void Dispose()
	{
		channelManager.ReleaseChannel(Channel);
	}
}
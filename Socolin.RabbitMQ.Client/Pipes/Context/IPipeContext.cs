using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Context
{
	public interface IPipeContext
	{
		public ChannelContainer? ChannelContainer { get; set; }
		public IModel? Channel { get; }
	}
}
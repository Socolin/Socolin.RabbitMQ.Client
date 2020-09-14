namespace Socolin.RabbitMQ.Client
{
	public interface IActiveConsumer
	{
		void Cancel();
	}

	public class ActiveConsumer : IActiveConsumer
	{
		public string ConsumerTag { get; }
		private readonly ChannelContainer _channelContainer;

		public ActiveConsumer(
			string consumerTag,
			ChannelContainer channelContainer
		)
		{
			ConsumerTag = consumerTag;
			_channelContainer = channelContainer;
		}

		public void Cancel()
		{
			_channelContainer.Channel.BasicCancel(ConsumerTag);
			_channelContainer.Dispose();
		}
	}
}
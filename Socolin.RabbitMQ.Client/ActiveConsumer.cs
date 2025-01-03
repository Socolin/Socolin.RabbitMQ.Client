using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer;

namespace Socolin.RabbitMQ.Client;

public interface IActiveConsumer
{
	Task CancelAsync();
	Task CancelAfterCurrentTaskCompletedAsync();
}

public class ActiveConsumer : IActiveConsumer
{
	public string ConsumerTag { get; }
	private readonly ChannelContainer _channelContainer;
	private readonly IActiveMessageProcessorCanceller _activeMessageProcessorCanceller;

	public ActiveConsumer(
		string consumerTag,
		ChannelContainer channelContainer,
		IActiveMessageProcessorCanceller activeMessageProcessorCanceller
	)
	{
		ConsumerTag = consumerTag;
		_channelContainer = channelContainer;
		_activeMessageProcessorCanceller = activeMessageProcessorCanceller;
	}

	public async Task CancelAsync()
	{
		_activeMessageProcessorCanceller.PreventStartProcessingNewMessage();
		await _channelContainer.Channel.BasicCancelAsync(ConsumerTag);
		_activeMessageProcessorCanceller.InterruptInProgressProcessor();
		_channelContainer.Dispose();
		await _activeMessageProcessorCanceller.WaitCurrentProcessingMessageToComplete();
	}

	public async Task CancelAfterCurrentTaskCompletedAsync()
	{
		_activeMessageProcessorCanceller.PreventStartProcessingNewMessage();
		await _channelContainer.Channel.BasicCancelAsync(ConsumerTag);
		await _activeMessageProcessorCanceller.WaitCurrentProcessingMessageToComplete();
		_channelContainer.Dispose();
	}
}
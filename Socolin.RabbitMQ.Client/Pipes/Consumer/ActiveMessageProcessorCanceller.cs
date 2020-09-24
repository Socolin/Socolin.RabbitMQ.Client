using System;
using System.Threading;
using System.Threading.Tasks;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public interface IActiveMessageProcessorCanceller
	{
		void InterruptInProgressProcessor();
		Task WaitCurrentProcessingMessageToComplete();
		void PreventStartProcessingNewMessage();
		CancellationToken StopProcessingNewMessageToken { get; }
		CancellationToken InterruptInProgressProcessorToken { get; }
		bool BeginProcessing();
		void EndProcessing();
		bool IsInterrupted();
	}

	public class ActiveMessageProcessorCanceller : IActiveMessageProcessorCanceller
	{
		public CancellationToken StopProcessingNewMessageToken => _stopProcessingNewMessage.Token;
		public CancellationToken InterruptInProgressProcessorToken => _interruptInProgressProcessor.Token;

		private readonly CancellationTokenSource _interruptInProgressProcessor = new CancellationTokenSource();
		private readonly CancellationTokenSource _stopProcessingNewMessage = new CancellationTokenSource();
		private readonly SemaphoreSlim _processingSemaphore = new SemaphoreSlim(1);

		void IActiveMessageProcessorCanceller.InterruptInProgressProcessor()
		{
			_interruptInProgressProcessor.Cancel();
		}

		public Task WaitCurrentProcessingMessageToComplete()
		{
			return _processingSemaphore.WaitAsync();
		}

		public void PreventStartProcessingNewMessage()
		{
			_stopProcessingNewMessage.Cancel();
		}

		public bool BeginProcessing()
		{
			return _processingSemaphore.Wait(TimeSpan.Zero);
		}

		public void EndProcessing()
		{
			_processingSemaphore.Release();
		}

		public bool IsInterrupted()
		{
			return InterruptInProgressProcessorToken.IsCancellationRequested;
		}
	}
}
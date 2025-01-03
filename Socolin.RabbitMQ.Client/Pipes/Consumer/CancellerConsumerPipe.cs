using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer;

public class CancellerConsumerPipe<T> : ConsumerPipe<T> where T : class
{
	public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
	{
		try
		{
			var alreadyWorking = context.ActiveMessageProcessorCanceller.BeginProcessing();
			if (context.ActiveMessageProcessorCanceller.StopProcessingNewMessageToken.IsCancellationRequested)
				return;
			if (!alreadyWorking)
				throw new ProcessingAlreadyInProgressException("A message processor is already in progress");
			await ProcessNextAsync(context, pipeline, cancellationToken);
		}
		finally
		{
			context.ActiveMessageProcessorCanceller.EndProcessing();
		}
	}
}
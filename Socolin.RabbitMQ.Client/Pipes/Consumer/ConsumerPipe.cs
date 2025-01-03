using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer;

public interface IConsumerPipe<T> where T : class
{
	Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default);
}

public abstract class ConsumerPipe<T> : IConsumerPipe<T> where T : class
{
	protected async Task ProcessNextAsync(
		IConsumerPipeContext<T> context,
		ReadOnlyMemory<IConsumerPipe<T>> pipeline,
		CancellationToken cancellationToken = default
	)
	{
		if (context.ActiveMessageProcessorCanceller.IsInterrupted())
			return;
		await ExecutePipelineAsync(context, pipeline, cancellationToken);
	}

	public static async Task ExecutePipelineAsync(
		IConsumerPipeContext<T> context,
		ReadOnlyMemory<IConsumerPipe<T>> pipeline,
		CancellationToken cancellationToken = default
	)
	{
		await pipeline.Span[0].ProcessAsync(context, pipeline.Slice(1), cancellationToken);
	}

	public abstract Task ProcessAsync(
		IConsumerPipeContext<T> context,
		ReadOnlyMemory<IConsumerPipe<T>> pipeline,
		CancellationToken cancellationToken = default
	);
}
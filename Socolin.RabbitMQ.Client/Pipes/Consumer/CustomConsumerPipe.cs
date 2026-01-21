using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer;

public class CustomConsumerPipe<T>(Func<IConsumerPipeContext<T>, Func<Task>, Task> pipeImpl) : ConsumerPipe<T>
	where T : class
{
	public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
	{
		Task Next() => ProcessNextAsync(context, pipeline, cancellationToken);
		await pipeImpl.Invoke(context, Next);
	}
}

public class CustomConsumerPipe2<T>(Func<IConsumerPipeContext<T>, Func<Task>, CancellationToken, Task> pipeImpl)
	: ConsumerPipe<T>
	where T : class
{
	public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
	{
		Task Next() => ProcessNextAsync(context, pipeline, cancellationToken);
		await pipeImpl.Invoke(context, Next, cancellationToken);
	}
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class CustomConsumerPipe<T> : ConsumerPipe<T> where T : class
	{
		private readonly Func<IConsumerPipeContext<T>, Func<Task>, Task> _pipeImpl;

		public CustomConsumerPipe(Func<IConsumerPipeContext<T>, Func<Task>, Task> pipeImpl)
		{
			_pipeImpl = pipeImpl;
		}

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
		{
			Task Next() => ProcessNextAsync(context, pipeline, cancellationToken);
			await _pipeImpl.Invoke(context, Next);
		}
	}
}
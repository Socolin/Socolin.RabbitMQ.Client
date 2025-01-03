using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class LogExceptionConsumerPipe<T>(LogExceptionConsumerPipe<T>.LogExceptionDelegate logFunc) : ConsumerPipe<T>
		where T : class
	{
		public delegate void LogExceptionDelegate(Exception exception, bool lastAttempt);

		private const string FinalAttemptItemsKey = "FinalAttempt";

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline, CancellationToken cancellationToken = default)
		{
			try
			{
				await ProcessNextAsync(context, pipeline, cancellationToken);
			}
			catch (Exception ex)
			{
				logFunc(ex, context.TryGetOptionalItemValue(FinalAttemptItemsKey, out bool finalAttempt) && finalAttempt);
				throw;
			}
		}
	}
}
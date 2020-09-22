using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer
{
	public class LogExceptionConsumerPipe<T> : ConsumerPipe<T> where T : class
	{
		public delegate void LogExceptionDelegate(Exception exception, bool lastAttempt);

		private const string FinalAttemptItemsKey = "FinalAttempt";

		private readonly LogExceptionDelegate _logFunc;

		public LogExceptionConsumerPipe(LogExceptionDelegate logFunc)
		{
			_logFunc = logFunc;
		}

		public override async Task ProcessAsync(IConsumerPipeContext<T> context, ReadOnlyMemory<IConsumerPipe<T>> pipeline)
		{
			try
			{
				await ProcessNextAsync(context, pipeline);
			}
			catch (Exception ex)
			{
				_logFunc(ex, context.TryGetOptionalItemValue(FinalAttemptItemsKey, out bool finalAttempt) && finalAttempt);
				throw;
			}
		}
	}
}
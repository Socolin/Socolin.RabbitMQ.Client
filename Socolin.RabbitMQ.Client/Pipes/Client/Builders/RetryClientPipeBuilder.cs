using System;
using Socolin.RabbitMQ.Client.Pipes.Client.Utils;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Builders
{
	public class RetryClientPipeBuilder : IGenericClientPipeBuilder
	{
		public TimeSpan? MaxRetryDuration { get; }
		public int? MaxRetryCount { get; }
		public TimeSpan? DelayBetweenRetry { get; }
		public TimeSpan[]? DelaysBetweenRetry { get; }

		public RetryClientPipeBuilder(TimeSpan? maxRetryDuration, int? maxRetryCount, TimeSpan delayBetweenRetry)
		{
			MaxRetryDuration = maxRetryDuration;
			MaxRetryCount = maxRetryCount;
			DelayBetweenRetry = delayBetweenRetry;
		}

		public RetryClientPipeBuilder(TimeSpan[]? delaysBetweenRetry)
		{
			DelaysBetweenRetry = delaysBetweenRetry;
		}

		public IGenericClientPipe BuildPipe()
		{
			if (DelaysBetweenRetry != null)
				return new RetryClientPipe(new ConnectionRetryWithDelaysUtil(DelaysBetweenRetry));

			return new RetryClientPipe(new ConnectionRetryUtil(this));
		}
	}
}
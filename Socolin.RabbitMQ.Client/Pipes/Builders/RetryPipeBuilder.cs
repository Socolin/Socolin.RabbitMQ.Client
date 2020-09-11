using System;
using Socolin.RabbitMQ.Client.Utils;

namespace Socolin.RabbitMQ.Client.Pipes.Builders
{
	public class RetryPipeBuilder : IGenericPipeBuilder
	{
		public TimeSpan? MaxRetryDuration { get; }
		public int? MaxRetryCount { get; }
		public TimeSpan DelayBetweenRetry { get; }

		public RetryPipeBuilder(TimeSpan? maxRetryDuration, int? maxRetryCount, TimeSpan delayBetweenRetry)
		{
			MaxRetryDuration = maxRetryDuration;
			MaxRetryCount = maxRetryCount;
			DelayBetweenRetry = delayBetweenRetry;
		}

		public IGenericPipe BuildPipe()
		{
			return new RetryPipe(new ConnectionRetryUtil(this));
		}
	}
}
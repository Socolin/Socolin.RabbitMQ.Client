using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Builders;

namespace Socolin.RabbitMQ.Client.Utils
{
	public interface IConnectionRetryUtil
	{
		Task ExecuteWithRetryOnErrorsAsync(Func<Task> work);
	}

	public class ConnectionRetryUtil : IConnectionRetryUtil
	{
		private readonly RetryPipeBuilder _retryPipeBuilder;

		public ConnectionRetryUtil(RetryPipeBuilder retryPipeBuilder)
		{
			_retryPipeBuilder = retryPipeBuilder;
		}

		public async Task ExecuteWithRetryOnErrorsAsync(Func<Task> work)
		{
			var retryCount = 0;
			var sw = Stopwatch.StartNew();

			while (true)
			{
				if (retryCount > 0)
					await Task.Delay(_retryPipeBuilder.DelayBetweenRetry);

				retryCount++;
				try
				{
					await work();
				}
				catch (AlreadyClosedException)
				{
					if (ShouldRetry(retryCount, sw))
						continue;
					throw;
				}
				catch (BrokerUnreachableException)
				{
					if (ShouldRetry(retryCount, sw))
						continue;
					throw;
				}

				break;
			}
		}

		private bool ShouldRetry(int retryCount, Stopwatch sw)
		{
			if (_retryPipeBuilder.MaxRetryCount.HasValue && retryCount <= _retryPipeBuilder.MaxRetryCount)
				return true;
			if (_retryPipeBuilder.MaxRetryDuration.HasValue)
				if (sw.ElapsedMilliseconds + _retryPipeBuilder.DelayBetweenRetry.TotalMilliseconds < _retryPipeBuilder.MaxRetryDuration.Value.TotalMilliseconds)
					return true;
			return false;
		}
	}
}
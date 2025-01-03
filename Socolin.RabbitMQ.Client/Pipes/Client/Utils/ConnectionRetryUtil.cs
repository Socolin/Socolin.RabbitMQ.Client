using System;
using System.Diagnostics;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Client.Builders;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Utils;

public interface IConnectionRetryUtil
{
	Task ExecuteWithRetryOnErrorsAsync(Func<Task> work);
}

public class ConnectionRetryUtil : IConnectionRetryUtil
{
	private readonly RetryClientPipeBuilder _retryClientPipeBuilder;

	public ConnectionRetryUtil(RetryClientPipeBuilder retryClientPipeBuilder)
	{
		_retryClientPipeBuilder = retryClientPipeBuilder;
		if (_retryClientPipeBuilder.DelayBetweenRetry == null)
			throw new ArgumentNullException(nameof(retryClientPipeBuilder), "DelayBetweenRetry is required");
	}

	public async Task ExecuteWithRetryOnErrorsAsync(Func<Task> work)
	{
		var retryCount = 0;
		var sw = Stopwatch.StartNew();

		while (true)
		{
			if (retryCount > 0)
				await Task.Delay(_retryClientPipeBuilder.DelayBetweenRetry!.Value);

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
		if (_retryClientPipeBuilder.MaxRetryCount.HasValue && retryCount <= _retryClientPipeBuilder.MaxRetryCount)
			return true;
		if (_retryClientPipeBuilder.MaxRetryDuration.HasValue)
			if (sw.ElapsedMilliseconds + _retryClientPipeBuilder.DelayBetweenRetry!.Value.TotalMilliseconds < _retryClientPipeBuilder.MaxRetryDuration.Value.TotalMilliseconds)
				return true;
		return false;
	}
}
using System;
using System.Threading.Tasks;
using RabbitMQ.Client.Exceptions;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Utils;

public class ConnectionRetryWithDelaysUtil : IConnectionRetryUtil
{
	private readonly TimeSpan[] _delaysBetweenRetry;

	public ConnectionRetryWithDelaysUtil(TimeSpan[] delaysBetweenRetry)
	{
		_delaysBetweenRetry = delaysBetweenRetry;
	}

	public async Task ExecuteWithRetryOnErrorsAsync(Func<Task> work)
	{
		var retryCount = 0;

		while (true)
		{
			if (retryCount > 0)
				await Task.Delay(_delaysBetweenRetry[retryCount - 1]);

			retryCount++;
			try
			{
				await work();
			}
			catch (AlreadyClosedException)
			{
				if (ShouldRetry(retryCount))
					continue;
				throw;
			}
			catch (BrokerUnreachableException)
			{
				if (ShouldRetry(retryCount))
					continue;
				throw;
			}

			break;
		}
	}

	private bool ShouldRetry(int retryCount)
	{
		if (retryCount <= _delaysBetweenRetry.Length)
			return true;

		return false;
	}
}
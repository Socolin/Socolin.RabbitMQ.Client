using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Context;
using Socolin.RabbitMQ.Client.Utils;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public class RetryPipe : Pipe, IGenericPipe
	{
		private readonly IConnectionRetryUtil _connectionRetryUtil;

		public RetryPipe(IConnectionRetryUtil connectionRetryUtil)
		{
			_connectionRetryUtil = connectionRetryUtil;
		}

		public async Task ProcessAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline)
		{
			await _connectionRetryUtil.ExecuteWithRetryOnErrorsAsync(() => ProcessNextAsync(context, pipeline));
		}
	}
}
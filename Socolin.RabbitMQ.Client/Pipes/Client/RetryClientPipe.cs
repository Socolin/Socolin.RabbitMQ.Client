using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;
using Socolin.RabbitMQ.Client.Pipes.Client.Utils;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class RetryClientPipe : ClientPipe, IGenericClientPipe
	{
		private readonly IConnectionRetryUtil _connectionRetryUtil;

		public RetryClientPipe(IConnectionRetryUtil connectionRetryUtil)
		{
			_connectionRetryUtil = connectionRetryUtil;
		}

		public async Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			await _connectionRetryUtil.ExecuteWithRetryOnErrorsAsync(() => ProcessNextAsync(context, pipeline));
		}
	}
}
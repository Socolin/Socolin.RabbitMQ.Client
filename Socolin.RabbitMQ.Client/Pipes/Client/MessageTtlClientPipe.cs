using System;
using System.Threading;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public class MessageTtlClientPipe(int? expiration) : ClientPipe, IMessageClientPipe
	{
		public const string ContextItemExpirationKey = "MessageTtlClientPipe.Expiration";
		private readonly string? _expiration = expiration?.ToString();

		public Task ProcessAsync(
			ClientPipeContextMessage context,
			ReadOnlyMemory<IClientPipe> pipeline,
			CancellationToken cancellation = default
		)
		{
			if (context.TryGetOptionalItemValue<int>(ContextItemExpirationKey, out var expiration))
				context.BasicProperties.Expiration = expiration.ToString();
			else if (_expiration != null)
				context.BasicProperties.Expiration = _expiration;

			return ProcessNextAsync(context, pipeline, cancellation);
		}
	}
}
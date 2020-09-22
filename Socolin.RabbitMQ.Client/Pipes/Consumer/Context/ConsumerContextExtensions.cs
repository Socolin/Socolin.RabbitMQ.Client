using Socolin.RabbitMQ.Client.Exceptions;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Context
{
	public static class ConsumerContextExtensions
	{
		public static bool TryGetOptionalItemValue<TValue, T>(this IConsumerPipeContext<T> context, string key, out TValue value) where T : class
		{
			value = default!;
			if (!context.Items.ContainsKey(key))
				return false;
			var obj = context.Items[key];
			if (!(obj is TValue v))
				throw new MissingItemInContextException(key, typeof(TValue));
			value = v;
			return true;
		}

		public static TValue GetItemValue<TValue, T>(this IConsumerPipeContext<T> context, string key) where T : class
		{
			if (!context.Items.ContainsKey(key))
				throw new MissingItemInContextException(key, typeof(TValue));
			var obj = context.Items[key];
			if (!(obj is TValue value))
				throw new MissingItemInContextException(key, typeof(TValue));
			return value;
		}
	}
}
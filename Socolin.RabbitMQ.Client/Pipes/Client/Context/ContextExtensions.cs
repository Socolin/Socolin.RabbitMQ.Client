using Socolin.RabbitMQ.Client.Exceptions;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public static class ContextExtensions
	{
		public static bool TryGetOptionalItemValue<T>(this IClientPipeContext context, string key, out T value)
		{
			value = default!;
			if (!context.Items.ContainsKey(key))
				return false;
			var obj = context.Items[key];
			if (!(obj is T v))
				throw new MissingItemInContextException(key, typeof(T));
			value = v;
			return true;
		}

		public static T GetItemValue<T>(this IClientPipeContext context, string key)
		{
			if (!context.Items.ContainsKey(key))
				throw new MissingItemInContextException(key, typeof(T));
			var obj = context.Items[key];
			if (!(obj is T value))
				throw new MissingItemInContextException(key, typeof(T));
			return value;
		}
	}
}
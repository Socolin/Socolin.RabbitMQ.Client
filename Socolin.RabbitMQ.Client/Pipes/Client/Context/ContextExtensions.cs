using Socolin.RabbitMQ.Client.Exceptions;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public static class ContextExtensions
	{
		public static bool TryGetOptionalItemValue<T>(this IClientPipeContext context, string key, out T value)
		{
			value = default!;
			if (!context.Items.TryGetValue(key, out var obj))
				return false;
			if (obj is not T v)
				throw new MissingItemInContextException(key, typeof(T));
			value = v;
			return true;
		}

		public static T GetItemValue<T>(this IClientPipeContext context, string key)
		{
			if (!context.Items.TryGetValue(key, out var obj))
				throw new MissingItemInContextException(key, typeof(T));
			if (obj is not T value)
				throw new MissingItemInContextException(key, typeof(T));
			return value;
		}
	}
}
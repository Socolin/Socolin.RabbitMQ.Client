using System;
using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options
{
	public class DeserializationGenericPipeOption
	{
		public readonly Func<Type, ReadOnlyMemory<byte>, object> DefaultDeserializer;
		public readonly IReadOnlyDictionary<string, Func<Type, ReadOnlyMemory<byte>, object>> Deserializers;

		public DeserializationGenericPipeOption(
			Func<Type, ReadOnlyMemory<byte>, object> defaultDeserializer,
			IReadOnlyDictionary<string, Func<Type, ReadOnlyMemory<byte>, object>> deserializers
		)
		{
			DefaultDeserializer = defaultDeserializer;
			Deserializers = deserializers;
		}
	}
}
using System;
using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options.Consumer
{
	public class DeserializationPipeOptions<T> where T : class
	{
		public readonly Func<ReadOnlyMemory<byte>, T> DefaultDeserializer;
		public readonly IReadOnlyDictionary<string, Func<ReadOnlyMemory<byte>, T>> Deserializers;

		public DeserializationPipeOptions(
			Func<ReadOnlyMemory<byte>, T> defaultDeserializer,
			IReadOnlyDictionary<string, Func<ReadOnlyMemory<byte>, T>> deserializers
		)
		{
			DefaultDeserializer = defaultDeserializer;
			Deserializers = deserializers;
		}
	}
}
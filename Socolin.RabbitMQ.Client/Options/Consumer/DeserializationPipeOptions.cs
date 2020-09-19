using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options.Consumer
{
	public class DeserializationPipeOptions<T> where T : class
	{
		public readonly SerializerDelegate<T> DefaultDeserializer;
		public readonly IReadOnlyDictionary<string, SerializerDelegate<T>> Deserializers;

		public DeserializationPipeOptions(
			SerializerDelegate<T> defaultDeserializer,
			IReadOnlyDictionary<string, SerializerDelegate<T>> deserializers
		)
		{
			DefaultDeserializer = defaultDeserializer;
			Deserializers = deserializers;
		}
	}
}
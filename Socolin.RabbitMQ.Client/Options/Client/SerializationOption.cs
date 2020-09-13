using System;

namespace Socolin.RabbitMQ.Client.Options.Client
{
	public class SerializationOption
	{
		public readonly Func<object, byte[]> Serializer;
		public readonly string? ContentType;

		public SerializationOption(Func<object, byte[]> serializer, string? contentType)
		{
			Serializer = serializer;
			ContentType = contentType;
		}
	}
}
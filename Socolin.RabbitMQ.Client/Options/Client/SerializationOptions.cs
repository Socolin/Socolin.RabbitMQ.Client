using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options.Client;

public delegate byte[] SerializerDelegate(object message);

public class SerializationOptions
{
	public readonly SerializerDelegate? Serializer;
	public readonly string? ContentType;
	public readonly Dictionary<string, SerializerDelegate> Serializers;

	public SerializationOptions(SerializerDelegate? serializer, string? contentType, Dictionary<string, SerializerDelegate> serializers)
	{
		Serializer = serializer;
		ContentType = contentType;
		Serializers = serializers;
	}
}
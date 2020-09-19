using System;

namespace Socolin.RabbitMQ.Client.Exceptions
{
	public class SerializerNotFoundException : Exception
	{
		public SerializerNotFoundException(string message) : base(message)
		{
		}
	}
}
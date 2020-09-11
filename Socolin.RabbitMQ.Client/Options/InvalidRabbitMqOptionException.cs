using System;

namespace Socolin.RabbitMQ.Client.Options
{
	public class InvalidRabbitMqOptionException : Exception
	{
		public InvalidRabbitMqOptionException(string message)
			: base(message)
		{
		}
	}
}
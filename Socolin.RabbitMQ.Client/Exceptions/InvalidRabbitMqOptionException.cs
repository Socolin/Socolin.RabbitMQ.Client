using System;

namespace Socolin.RabbitMQ.Client.Exceptions;

public class InvalidRabbitMqOptionException : Exception
{
	public InvalidRabbitMqOptionException(string message)
		: base(message)
	{
	}
}
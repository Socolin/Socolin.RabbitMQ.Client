using System;

namespace Socolin.RabbitMQ.Client.Exceptions;

public class RabbitMqPipeException : Exception
{
	public RabbitMqPipeException(string message) : base(message)
	{
	}
}
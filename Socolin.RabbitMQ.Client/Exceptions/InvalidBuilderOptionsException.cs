using System;

namespace Socolin.RabbitMQ.Client.Exceptions;

public class InvalidBuilderOptionsException : Exception
{
	public InvalidBuilderOptionsException(string message)
		: base(message)
	{
	}
}
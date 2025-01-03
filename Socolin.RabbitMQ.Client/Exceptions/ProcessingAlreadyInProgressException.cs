using System;

namespace Socolin.RabbitMQ.Client.Exceptions;

public class ProcessingAlreadyInProgressException : Exception
{
	public ProcessingAlreadyInProgressException(string message) : base(message)
	{
	}
}
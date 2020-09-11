using System;
using System.Reflection;

namespace Socolin.RabbitMQ.Client.Exceptions
{
	public class MissingItemInContextException : Exception
	{
		public MissingItemInContextException(string key, MemberInfo type)
			: base($"No element for the key `{key}` of type {type.Name} was found in context")
		{
		}
	}
}
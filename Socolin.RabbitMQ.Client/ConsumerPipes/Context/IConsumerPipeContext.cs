using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Socolin.RabbitMQ.Client.ConsumerPipes.Context
{
	public interface IConsumerPipeContext<T> where T : class
	{
		IModel Chanel { get; }
		BasicDeliverEventArgs RabbitMqMessage { get; }
		T? DeserializedMessage { get; set; }

		/// <summary>
		/// A function called at the end of the pipe to process the deserialized message
		/// </summary>
		Func<T, Dictionary<string, object>, Task> MessageProcessor { get; }

		/// <summary>
		/// A bag to store data that need to be between multiple pipe or accessed at the end
		/// </summary>
		Dictionary<string, object> Items { get; }
	}
}
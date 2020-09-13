using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Context
{
	public class ConsumerPipeContext<T> : IConsumerPipeContext<T> where T : class
	{
		public IModel Chanel { get; }
		public T? DeserializedMessage { get; set; }
		public BasicDeliverEventArgs RabbitMqMessage { get; }
		public Func<T, Dictionary<string, object>, Task> MessageProcessor { get; }
		public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

		public ConsumerPipeContext(IModel chanel, BasicDeliverEventArgs basicDeliverEventArgs, Func<T, Dictionary<string, object>, Task> messageProcessor)
		{
			RabbitMqMessage = basicDeliverEventArgs;
			MessageProcessor = messageProcessor;
			Chanel = chanel;
		}
	}
}
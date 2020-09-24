using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Context
{
	public class ConsumerPipeContext<T> : IConsumerPipeContext<T> where T : class
	{
		public IModel Chanel { get; }
		public T? DeserializedMessage { get; set; }
		public BasicDeliverEventArgs RabbitMqMessage { get; }
		public ProcessorMessageDelegate<T> MessageProcessor { get; }
		public IActiveMessageProcessorCanceller ActiveMessageProcessorCanceller { get; }
		public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

		public ConsumerPipeContext(
			IModel chanel,
			BasicDeliverEventArgs basicDeliverEventArgs,
			ProcessorMessageDelegate<T> messageProcessor,
			IActiveMessageProcessorCanceller activeMessageProcessorCanceller
		)
		{
			RabbitMqMessage = basicDeliverEventArgs;
			MessageProcessor = messageProcessor;
			ActiveMessageProcessorCanceller = activeMessageProcessorCanceller;
			Chanel = chanel;
		}
	}
}
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Context
{
	public class ConsumerPipeContext<T>(
		IRabbitMqConnectionManager connectionManager,
		IChannel chanel,
		BasicDeliverEventArgs basicDeliverEventArgs,
		ProcessorMessageDelegate<T> messageProcessor,
		IActiveMessageProcessorCanceller activeMessageProcessorCanceller
	) : IConsumerPipeContext<T>
		where T : class
	{
		public IRabbitMqConnectionManager ConnectionManager { get; } = connectionManager;
		public IChannel Chanel { get; } = chanel;
		public T? DeserializedMessage { get; set; }
		public BasicDeliverEventArgs RabbitMqMessage { get; } = basicDeliverEventArgs;
		public ProcessorMessageDelegate<T> MessageProcessor { get; } = messageProcessor;
		public IActiveMessageProcessorCanceller ActiveMessageProcessorCanceller { get; } = activeMessageProcessorCanceller;
		public Dictionary<string, object> Items { get; } = new();
	}
}
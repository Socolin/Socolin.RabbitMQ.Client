using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Context
{
	public delegate Task ProcessorMessageDelegate<in T>(T message, Dictionary<string, object> items, CancellationToken cancellationToken) where T : class;

	public interface IConsumerPipeContext<T> where T : class
	{
		IRabbitMqConnectionManager ConnectionManager { get; }
		IChannel Chanel { get; }
		BasicDeliverEventArgs RabbitMqMessage { get; }
		T? DeserializedMessage { get; set; }
		IActiveMessageProcessorCanceller ActiveMessageProcessorCanceller { get; }

		/// <summary>
		/// A function called at the end of the pipe to process the deserialized message
		/// </summary>
		ProcessorMessageDelegate<T> MessageProcessor { get; }

		/// <summary>
		/// A bag to store data that need to be between multiple pipe or accessed at the end
		/// </summary>
		Dictionary<string, object> Items { get; }
	}
}
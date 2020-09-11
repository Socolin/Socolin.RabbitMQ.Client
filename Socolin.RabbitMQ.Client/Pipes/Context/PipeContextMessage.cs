using System;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Context
{
	public class PipeContextMessage : IPipeContext
	{
		public object Message { get; set; }
		public string? QueueName { get; set; }
		public IBasicProperties? BasicProperties { get; set; }
		public ReadOnlyMemory<byte> SerializedMessage { get; set; }

		public PipeContextMessage(object message)
		{
			Message = message;
		}

		public ChannelContainer? ChannelContainer { get; set; }
		public IModel? Channel => ChannelContainer?.Channel;
	}
}
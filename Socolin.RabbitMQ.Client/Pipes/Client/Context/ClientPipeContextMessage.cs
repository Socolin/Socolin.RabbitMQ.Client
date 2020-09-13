using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public class ClientPipeContextMessage : IClientPipeContext
	{
		public object Message { get; set; }
		public string? RoutingKey { get; set; }
		public string ExchangeName { get; set; } = RabbitMqConstants.DefaultExchangeName;
		public IBasicProperties? BasicProperties { get; set; }
		public ReadOnlyMemory<byte> SerializedMessage { get; set; }
		public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

		public ClientPipeContextMessage(object message)
		{
			Message = message;
		}

		public ChannelContainer? ChannelContainer { get; set; }
		public IModel? Channel => ChannelContainer?.Channel;
	}
}
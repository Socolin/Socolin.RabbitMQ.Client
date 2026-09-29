using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context;

public class ClientPipeContextMessage : IClientPipeContext
{
	public object Message { get; set; }
	public string RoutingKey { get; set; } = string.Empty;
	public string ExchangeName { get; set; } = RabbitMqConstants.DefaultExchangeName;
	public bool Mandatory { get; set; } = true;
	public BasicProperties BasicProperties { get; set; } = new();
	public ReadOnlyMemory<byte> SerializedMessage { get; set; }
	public Dictionary<string, object> Items { get; }

	public ClientPipeContextMessage(object message, Dictionary<string, object>? items = null)
	{
		Message = message;
		Items = items ?? new Dictionary<string, object>();
	}

	internal void SetPriority(byte? priority)
	{
		if (priority > 31)
			throw new ArgumentOutOfRangeException(nameof(priority), priority, "Priority must be between 0 and 31.");

		if (priority.HasValue)
			BasicProperties.Priority = priority.Value;
	}

	public ChannelContainer? ChannelContainer { get; set; }
	public IChannel? Channel => ChannelContainer?.Channel;
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Exceptions;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public class ClientPipeContextAction : IClientPipeContext
	{
		public ChannelContainer? ChannelContainer { get; set; }
		public IModel? Channel => ChannelContainer?.Channel;
		public Func<IModel, ClientPipeContextAction, Task> Action { get; }
		public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

		public ClientPipeContextAction(Func<IModel, ClientPipeContextAction, Task> action)
		{
			Action = action;
		}

		public T GetItemValue<T>(string key)
		{
			if (!Items.ContainsKey(key))
				throw new MissingItemInContextException(key, typeof(T));
			var obj = Items[key];
			if (!(obj is T value))
				throw new MissingItemInContextException(key, typeof(T));
			return value;
		}
	}
}
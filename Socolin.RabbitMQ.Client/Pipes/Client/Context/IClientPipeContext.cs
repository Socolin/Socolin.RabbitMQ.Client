using System.Collections.Generic;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public interface IClientPipeContext
	{
		public ChannelContainer? ChannelContainer { set; }
		public IModel? Channel { get; }

		/// <summary>
		/// A bag to store data that need to be between multiple pipe or accessed at the end
		/// </summary>
		public Dictionary<string, object> Items { get; }
	}
}
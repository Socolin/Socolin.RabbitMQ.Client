using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public class ClientPipeContextAction(ClientPipeContextAction.ActionDelegate action) : IClientPipeContext
	{
		public delegate Task ActionDelegate(IChannel channel, ClientPipeContextAction context);

		public ChannelContainer? ChannelContainer { get; set; }
		public IChannel? Channel => ChannelContainer?.Channel;
		public ActionDelegate Action { get; } = action;
		public Dictionary<string, object> Items { get; } = new();
	}
}
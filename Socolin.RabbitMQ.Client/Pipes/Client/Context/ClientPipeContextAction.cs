using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Pipes.Client.Context
{
	public class ClientPipeContextAction : IClientPipeContext
	{
		public delegate Task ActionDelegate(IModel channel, ClientPipeContextAction context);

		public ChannelContainer? ChannelContainer { get; set; }
		public IModel? Channel => ChannelContainer?.Channel;
		public ActionDelegate Action { get; }
		public Dictionary<string, object> Items { get; } = new Dictionary<string, object>();

		public ClientPipeContextAction(ActionDelegate action)
		{
			Action = action;
		}
	}
}
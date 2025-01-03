using System.Collections.Generic;

namespace Socolin.RabbitMQ.Client.Options.Client
{
	public class CreateQueueOptions
	{
		public bool Durable { get; set; }
		public bool AutoDelete { get; set; }
		public bool Exclusive { get; set; }
		public IDictionary<string, object?>? Arguments { get; set; }
	}
}
namespace Socolin.RabbitMQ.Client.Options.Client
{
	public class PerMessageTtlOption
	{
		public readonly int? PerMessageTTl;

		public PerMessageTtlOption(int? perMessageTTl)
		{
			PerMessageTTl = perMessageTTl;
		}
	}
}
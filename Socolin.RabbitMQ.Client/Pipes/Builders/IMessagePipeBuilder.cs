namespace Socolin.RabbitMQ.Client.Pipes.Builders
{
	public interface IMessagePipeBuilder : IPipeBuilder
	{
		IMessagePipe BuildPipe();
	}
}
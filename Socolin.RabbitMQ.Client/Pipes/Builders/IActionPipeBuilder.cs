namespace Socolin.RabbitMQ.Client.Pipes.Builders
{
	public interface IActionPipeBuilder : IPipeBuilder
	{
		IActionPipe BuildPipe();
	}
}
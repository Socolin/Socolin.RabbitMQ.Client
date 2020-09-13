namespace Socolin.RabbitMQ.Client.Pipes.Client.Builders
{
	public interface IActionClientPipeBuilder : IClientPipeBuilder
	{
		IActionClientPipe BuildPipe();
	}
}
namespace Socolin.RabbitMQ.Client.Pipes.Builders
{
	public interface IGenericPipeBuilder : IPipeBuilder
	{
		IGenericPipe BuildPipe();
	}
}
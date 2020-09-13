namespace Socolin.RabbitMQ.Client.Pipes.Client.Builders
{
	public interface IGenericClientPipeBuilder : IClientPipeBuilder
	{
		IGenericClientPipe BuildPipe();
	}
}
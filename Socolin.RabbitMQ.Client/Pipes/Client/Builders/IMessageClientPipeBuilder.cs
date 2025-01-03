namespace Socolin.RabbitMQ.Client.Pipes.Client.Builders;

public interface IMessageClientPipeBuilder : IClientPipeBuilder
{
	IMessageClientPipe BuildPipe();
}
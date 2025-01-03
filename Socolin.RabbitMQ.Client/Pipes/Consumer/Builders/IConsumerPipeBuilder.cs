namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Builders;

public interface IConsumerPipeBuilder<T> where T : class
{
	IConsumerPipe<T> Build();
}
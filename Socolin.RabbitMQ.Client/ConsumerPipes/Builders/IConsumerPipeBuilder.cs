namespace Socolin.RabbitMQ.Client.ConsumerPipes.Builders
{
	public interface IConsumerPipeBuilder<T> where T : class
	{
		IConsumerPipe<T> Build();
	}
}
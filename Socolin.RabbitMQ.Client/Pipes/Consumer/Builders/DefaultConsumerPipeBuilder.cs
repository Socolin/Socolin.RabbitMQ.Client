using System;

namespace Socolin.RabbitMQ.Client.Pipes.Consumer.Builders;

public class DefaultConsumerPipeBuilder<T> : IConsumerPipeBuilder<T> where T : class
{
	private readonly Func<IConsumerPipe<T>> _build;

	public DefaultConsumerPipeBuilder(Func<IConsumerPipe<T>> build)
	{
		_build = build;
	}

	public IConsumerPipe<T> Build()
	{
		return _build.Invoke();
	}
}
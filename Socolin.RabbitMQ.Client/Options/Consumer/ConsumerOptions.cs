using System;
using System.Collections.Generic;
using System.Linq;
using Socolin.RabbitMQ.Client.Pipes.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Builders;

namespace Socolin.RabbitMQ.Client.Options.Consumer
{
	public class ConsumerOptions<T> where T : class
	{
		public DeserializationPipeOptions<T> Deserialization { get; set; }
		public List<IConsumerPipeBuilder<T>> Customs { get; set; } = new List<IConsumerPipeBuilder<T>>();
		public IConsumerPipeBuilder<T>? MessageAcknowledgmentPipeBuilder { get; set; }
		public LogExceptionConsumerPipe<T>.LogExceptionDelegate? LogDelegate { get; set; }
		public ushort? PrefetchCount { get; set; }

		public ConsumerOptions(DeserializationPipeOptions<T> deserialization)
		{
			Deserialization = deserialization;
		}

		public ReadOnlyMemory<IConsumerPipe<T>> BuildPipeline()
		{
			var pipeline = new List<IConsumerPipe<T>>
			{
				new CancellerConsumerPipe<T>()
			};

			if (MessageAcknowledgmentPipeBuilder != null)
				pipeline.Add(MessageAcknowledgmentPipeBuilder.Build());
			if (LogDelegate != null)
				pipeline.Add(new LogExceptionConsumerPipe<T>(LogDelegate));
			pipeline.Add(new DeserializerConsumerPipe<T>(Deserialization));

			pipeline.AddRange(Customs.Select(builder => builder.Build()));
			pipeline.Add(new MessageProcessorPipe<T>());

			return pipeline.ToArray();
		}
	}
}
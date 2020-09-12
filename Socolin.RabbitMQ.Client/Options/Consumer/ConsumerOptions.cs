using System;
using System.Collections.Generic;
using System.Linq;
using Socolin.RabbitMQ.Client.ConsumerPipes;
using Socolin.RabbitMQ.Client.ConsumerPipes.Builders;

namespace Socolin.RabbitMQ.Client.Options.Consumer
{
	public class ConsumerOptions<T> where T : class
	{
		public DeserializationPipeOptions<T> Deserialization { get; set; }
		public List<IConsumerPipeBuilder<T>> Customs { get; set; } = new List<IConsumerPipeBuilder<T>>();
		public IConsumerPipeBuilder<T>? MessageAcknowledgmentPipeBuilder { get; set; }

		public ConsumerOptions(DeserializationPipeOptions<T> deserialization)
		{
			Deserialization = deserialization;
		}

		public ReadOnlyMemory<IConsumerPipe<T>> BuildPipeline()
		{
			var pipeline = new List<IConsumerPipe<T>>
			{
				new DeserializerConsumerPipe<T>(Deserialization)
			};

			if (MessageAcknowledgmentPipeBuilder != null)
				pipeline.Add(MessageAcknowledgmentPipeBuilder.Build());
			pipeline.AddRange(Customs.Select(builder => builder.Build()));
			pipeline.Add(new MessageProcessorPipe<T>());

			return pipeline.ToArray();
		}
	}
}
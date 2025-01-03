using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMQ.Client;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Builders;

namespace Socolin.RabbitMQ.Client.Options.Client
{
	public class RabbitMqServiceClientOptions
	{
		public IRabbitMqConnectionManager RabbitMqConnectionManager { get; set; }
		public SerializationOptions? Serialization { get; set; }
		public IGenericClientPipeBuilder? Retry { get; set; }
		public List<IClientPipeBuilder> CustomPipes { get; set; } = [];
		public PerMessageTtlOption? PerMessageTtl { get; set; }
		public DeliveryModes? DeliveryMode { get; set; }

		public RabbitMqServiceClientOptions(
			IRabbitMqConnectionManager rabbitMqConnectionManager
		)
		{
			RabbitMqConnectionManager = rabbitMqConnectionManager;
		}

		public ReadOnlyMemory<IClientPipe> BuildMessagePipeline()
		{
			if (Serialization == null)
				throw new InvalidRabbitMqOptionException("Please provide serialization to build message pipeline");
			var pipes = new List<IClientPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new ConnectionClientPipe(RabbitMqConnectionManager, ChannelType.Publish));
			pipes.Add(new SerializerClientPipe(Serialization));
			if (PerMessageTtl != null)
				pipes.Add(new MessageTtlClientPipe(PerMessageTtl.PerMessageTTl));
			pipes.AddRange(CustomPipes
				.Where(builder => builder is IMessageClientPipeBuilder or IGenericClientPipeBuilder)
				.Select(IClientPipe (builder) =>
				{
					if (builder is IMessageClientPipeBuilder messagePipeBuilder)
						return messagePipeBuilder.BuildPipe();
					if (builder is IGenericClientPipeBuilder genericPipeBuilder)
						return genericPipeBuilder.BuildPipe();
					throw new NotSupportedException($"Builder {builder} is not supported");
				})
			);
			pipes.Add(new PublishClientPipe(DeliveryMode));

			return new ReadOnlyMemory<IClientPipe>(pipes.ToArray());
		}

		public ReadOnlyMemory<IClientPipe> BuildActionPipeline()
		{
			var pipes = new List<IClientPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new ConnectionClientPipe(RabbitMqConnectionManager, ChannelType.Publish));
			pipes.AddRange(CustomPipes
				.Where(builder => builder is IActionClientPipeBuilder || builder is IGenericClientPipeBuilder)
				.Select(IClientPipe (builder) =>
				{
					if (builder is IActionClientPipeBuilder messagePipeBuilder)
						return messagePipeBuilder.BuildPipe();
					if (builder is IGenericClientPipeBuilder genericPipeBuilder)
						return genericPipeBuilder.BuildPipe();
					throw new NotSupportedException($"Builder {builder} is not supported");
				})
			);
			pipes.Add(new ExecuteActionClientPipe());

			return new ReadOnlyMemory<IClientPipe>(pipes.ToArray());
		}

		public ReadOnlyMemory<IClientPipe> BuildConsumerPipeline()
		{
			var pipes = new List<IClientPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new PersistentConnectionClientPipe(RabbitMqConnectionManager, ChannelType.Consumer));
			pipes.Add(new ExecuteActionClientPipe());

			return new ReadOnlyMemory<IClientPipe>(pipes.ToArray());
		}
	}
}
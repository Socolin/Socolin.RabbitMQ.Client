using System;
using System.Collections.Generic;
using System.Linq;
using Socolin.RabbitMQ.Client.Pipes;
using Socolin.RabbitMQ.Client.Pipes.Builders;

namespace Socolin.RabbitMQ.Client.Options
{
	public class RabbitMqServiceClientOptions
	{
		public IRabbitMqConnectionManager RabbitMqConnectionManager { get; set; }
		public SerializationOption? Serialization { get; set; }
		public IGenericPipeBuilder? Retry { get; set; }
		public List<IPipeBuilder> CustomPipes { get; set; } = new List<IPipeBuilder>();

		public RabbitMqServiceClientOptions(
			IRabbitMqConnectionManager rabbitMqConnectionManager
		)
		{
			RabbitMqConnectionManager = rabbitMqConnectionManager;
		}

		public ReadOnlyMemory<IPipe> BuildMessagePipeline()
		{
			if (Serialization == null)
				throw new InvalidRabbitMqOptionException("Please provide serialization to build message pipeline");
			var pipes = new List<IPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new ConnectionPipe(RabbitMqConnectionManager));
			pipes.Add(new SerializerPipe(Serialization.Serializer, Serialization.ContentType));
			pipes.AddRange(CustomPipes
				.Where(builder => builder is IMessagePipeBuilder || builder is IGenericPipeBuilder)
				.Select(builder =>
				{
					if (builder is IMessagePipeBuilder messagePipeBuilder)
						return messagePipeBuilder.BuildPipe() as IPipe;
					if (builder is IGenericPipeBuilder genericPipeBuilder)
						return genericPipeBuilder.BuildPipe() as IPipe;
					throw new NotSupportedException($"Builder {builder} is not supported");
				})
			);
			pipes.Add(new PublishPipe());

			return new ReadOnlyMemory<IPipe>(pipes.ToArray());
		}

		public ReadOnlyMemory<IPipe> BuildActionPipeline()
		{
			var pipes = new List<IPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new ConnectionPipe(RabbitMqConnectionManager));
			pipes.AddRange(CustomPipes
				.Where(builder => builder is IActionPipeBuilder || builder is IGenericPipeBuilder)
				.Select(builder =>
				{
					if (builder is IActionPipeBuilder messagePipeBuilder)
						return messagePipeBuilder.BuildPipe() as IPipe;
					if (builder is IGenericPipeBuilder genericPipeBuilder)
						return genericPipeBuilder.BuildPipe() as IPipe;
					throw new NotSupportedException($"Builder {builder} is not supported");
				})
			);
			pipes.Add(new ExecuteActionPipe());

			return new ReadOnlyMemory<IPipe>(pipes.ToArray());
		}

		public ReadOnlyMemory<IPipe> BuildConsumerPipeline()
		{
			var pipes = new List<IPipe>();

			if (Retry != null)
				pipes.Add(Retry.BuildPipe());
			pipes.Add(new PersistentConnectionPipe(RabbitMqConnectionManager));
			pipes.Add(new ExecuteActionPipe());

			return new ReadOnlyMemory<IPipe>(pipes.ToArray());
		}
	}
}
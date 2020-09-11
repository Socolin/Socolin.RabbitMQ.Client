using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client.Pipes
{
	public interface IPipe
	{
		public Task ProcessNextAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline);
	}

	public interface IGenericPipe : IPipe
	{
		public Task ProcessAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline);
	}

	public interface IActionPipe : IPipe
	{
		public Task ProcessAsync(PipeContextAction pipeContextAction, ReadOnlyMemory<IPipe> pipeline);
	}

	public interface IMessagePipe : IPipe
	{
		public Task ProcessAsync(PipeContextMessage pipeContextMessage, ReadOnlyMemory<IPipe> pipeline);
	}

	public abstract class Pipe : IPipe
	{
		public Task ProcessNextAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline)
		{
			return ExecutePipelineAsync(context, pipeline);
		}

		public static Task ExecutePipelineAsync(IPipeContext context, ReadOnlyMemory<IPipe> pipeline)
		{
			var pipe = pipeline.Span[0];
			if (pipe is IGenericPipe genericPipe)
				return genericPipe.ProcessAsync(context, pipeline.Slice(1));
			if (context is PipeContextAction contextAction && pipe is IActionPipe actionPipe)
				return actionPipe.ProcessAsync(contextAction, pipeline.Slice(1));
			if (context is PipeContextMessage contextMessage && pipe is IMessagePipe messagePipe)
				return messagePipe.ProcessAsync(contextMessage, pipeline.Slice(1));
			throw new RabbitMqPipeException($"Unsupported message of type {context.GetType()} for pipe {pipeline.GetType()}");
		}
	}
}
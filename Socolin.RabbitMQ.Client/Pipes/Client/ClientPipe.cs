using System;
using System.Threading.Tasks;
using Socolin.RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;

namespace Socolin.RabbitMQ.Client.Pipes.Client
{
	public interface IClientPipe
	{
	}

	public interface IGenericClientPipe : IClientPipe
	{
		public Task ProcessAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline);
	}

	public interface IActionClientPipe : IClientPipe
	{
		public Task ProcessAsync(ClientPipeContextAction clientPipeContextAction, ReadOnlyMemory<IClientPipe> pipeline);
	}

	public interface IMessageClientPipe : IClientPipe
	{
		public Task ProcessAsync(ClientPipeContextMessage clientPipeContextMessage, ReadOnlyMemory<IClientPipe> pipeline);
	}

	public abstract class ClientPipe : IClientPipe
	{
		protected Task ProcessNextAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			return ExecutePipelineAsync(context, pipeline);
		}

		public static Task ExecutePipelineAsync(IClientPipeContext context, ReadOnlyMemory<IClientPipe> pipeline)
		{
			var pipe = pipeline.Span[0];
			if (pipe is IGenericClientPipe genericPipe)
				return genericPipe.ProcessAsync(context, pipeline.Slice(1));
			if (context is ClientPipeContextAction contextAction && pipe is IActionClientPipe actionPipe)
				return actionPipe.ProcessAsync(contextAction, pipeline.Slice(1));
			if (context is ClientPipeContextMessage contextMessage && pipe is IMessageClientPipe messagePipe)
				return messagePipe.ProcessAsync(contextMessage, pipeline.Slice(1));
			throw new RabbitMqPipeException($"Unsupported message of type {context.GetType()} for pipe {pipeline.GetType()}");
		}
	}
}
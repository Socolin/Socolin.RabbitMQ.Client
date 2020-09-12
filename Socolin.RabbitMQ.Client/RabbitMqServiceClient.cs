using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.ConsumerPipes;
using Socolin.RabbitMQ.Client.ConsumerPipes.Context;
using Socolin.RabbitMQ.Client.Options;
using Socolin.RabbitMQ.Client.Options.Consumer;
using Socolin.RabbitMQ.Client.Pipes;
using Socolin.RabbitMQ.Client.Pipes.Context;

namespace Socolin.RabbitMQ.Client
{
	public interface IRabbitMqServiceClient
	{
		Task CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null);
		Task PurgeQueueAsync(string queueName);
		Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty);
		Task EnqueueMessageAsync(string queueName, object message);
		Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, ConsumerOptions<T> consumerOptions, Func<T, Dictionary<string, object>, Task> messageProcessor) where T : class;
		RabbitMqEnqueueQueueClient CreateQueueClient(string queueName);
	}

	public class RabbitMqServiceClient : IRabbitMqServiceClient
	{
		private readonly Lazy<ReadOnlyMemory<IPipe>> _messagePipeline;
		private readonly Lazy<ReadOnlyMemory<IPipe>> _actionPipeline;
		private readonly Lazy<ReadOnlyMemory<IPipe>> _consumerPipeline;

		public RabbitMqServiceClient(RabbitMqServiceClientOptions options)
		{
			_messagePipeline = new Lazy<ReadOnlyMemory<IPipe>>(options.BuildMessagePipeline);
			_actionPipeline = new Lazy<ReadOnlyMemory<IPipe>>(options.BuildActionPipeline);
			_consumerPipeline = new Lazy<ReadOnlyMemory<IPipe>>(options.BuildConsumerPipeline);
		}

		public async Task CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueueDeclare(queueName, durable, exclusive, autoDelete, arguments);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task PurgeQueueAsync(string queueName)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueuePurge(queueName);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task<long> GetMessageCountInQueueAsync(string queueName)
		{
			const string messageCountKey = "messageCount";
			var pipeContextAction = new PipeContextAction((channel, context) =>
			{
				try
				{
					context.Items[messageCountKey] = (long) channel.QueueDeclarePassive(queueName).MessageCount;
				}
				catch (OperationInterruptedException ex) when (ex.ShutdownReason.ReplyCode == 404)
				{
					context.Items[messageCountKey] = -1L;
				}

				return Task.CompletedTask;
			});
			await Pipe.ExecutePipelineAsync(pipeContextAction, _actionPipeline.Value);

			return pipeContextAction.GetItemValue<long>(messageCountKey);
		}

		public async Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueueDelete(queueName, ifUnused, ifEmpty);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task EnqueueMessageAsync(string queueName, object message)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextMessage(message) {QueueName = queueName}, _messagePipeline.Value);
		}

		public async Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, ConsumerOptions<T> consumerOptions, Func<T, Dictionary<string, object>, Task> messageProcessor) where T : class
		{
			var consumerPipeline = consumerOptions.BuildPipeline();

			const string consumerTagKey = "consumerTag";
			var pipeContext = new PipeContextAction((channel, context) =>
			{
				var consumerTag = BeginConsumeQueue(channel, queueName, consumerPipeline, messageProcessor);
				context.Items[consumerTagKey] = consumerTag;
				return Task.CompletedTask;
			});
			await Pipe.ExecutePipelineAsync(pipeContext, _consumerPipeline.Value);

			return new ActiveConsumer(pipeContext.GetItemValue<string>(consumerTagKey), pipeContext.ChannelContainer!);
		}

		private string BeginConsumeQueue<T>(IModel channel, string queueName, ReadOnlyMemory<IConsumerPipe<T>> consumerPipeline, Func<T, Dictionary<string, object>, Task> messageProcessor) where T : class
		{
			var consumer = new AsyncEventingBasicConsumer(channel);
			consumer.Received += async (_, message) =>
			{
				var consumerPipeContext = new ConsumerPipeContext<T>(channel, message, messageProcessor);
				await ConsumerPipe<T>.ExecutePipelineAsync(consumerPipeContext, consumerPipeline);
			};

			return channel.BasicConsume(queueName, false, consumer);
		}

		public RabbitMqEnqueueQueueClient CreateQueueClient(string queueName)
		{
			var queueClientPipe = new List<IPipe>(_messagePipeline.Value.Span.ToArray());
			queueClientPipe.Insert(0, new QueueSelectionPipe(queueName));
			return new RabbitMqEnqueueQueueClient(new ReadOnlyMemory<IPipe>(queueClientPipe.ToArray()));
		}
	}
}
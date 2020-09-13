using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Client;
using Socolin.RabbitMQ.Client.Pipes.Client.Context;
using Socolin.RabbitMQ.Client.Pipes.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client
{
	public interface IRabbitMqServiceClient
	{
		Task CreateQueueAsync(string queueName, CreateQueueOptions options);
		Task CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null);
		Task PurgeQueueAsync(string queueName);
		Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty);
		Task EnqueueMessageAsync(string queueName, object message);
		Task EnqueueMessageToExchangeAsync(string exchangeName, string routingKey, object message);
		Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, ConsumerOptions<T> consumerOptions, Func<T, Dictionary<string, object>, Task> messageProcessor) where T : class;
		RabbitMqEnqueueQueueClient CreateQueueClient(string queueName);
		RabbitMqEnqueueQueueClient CreateQueueClient(string exchangeName, string routingKey);
	}

	public class RabbitMqServiceClient : IRabbitMqServiceClient
	{
		private readonly Lazy<ReadOnlyMemory<IClientPipe>> _messagePipeline;
		private readonly Lazy<ReadOnlyMemory<IClientPipe>> _actionPipeline;
		private readonly Lazy<ReadOnlyMemory<IClientPipe>> _consumerPipeline;

		public RabbitMqServiceClient(RabbitMqServiceClientOptions options)
		{
			_messagePipeline = new Lazy<ReadOnlyMemory<IClientPipe>>(options.BuildMessagePipeline);
			_actionPipeline = new Lazy<ReadOnlyMemory<IClientPipe>>(options.BuildActionPipeline);
			_consumerPipeline = new Lazy<ReadOnlyMemory<IClientPipe>>(options.BuildConsumerPipeline);
		}

		public Task CreateQueueAsync(string queueName, CreateQueueOptions options)
		{
			return CreateQueueAsync(queueName, options.Durable, options.Exclusive, options.AutoDelete, options.Arguments);
		}

		public async Task CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null)
		{
			await ClientPipe.ExecutePipelineAsync(new ClientPipeContextAction((channel, _) =>
			{
				channel.QueueDeclare(queueName, durable, exclusive, autoDelete, arguments);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task PurgeQueueAsync(string queueName)
		{
			await ClientPipe.ExecutePipelineAsync(new ClientPipeContextAction((channel, _) =>
			{
				channel.QueuePurge(queueName);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task<long> GetMessageCountInQueueAsync(string queueName)
		{
			const string messageCountKey = "messageCount";
			var pipeContextAction = new ClientPipeContextAction((channel, context) =>
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
			await ClientPipe.ExecutePipelineAsync(pipeContextAction, _actionPipeline.Value);

			return pipeContextAction.GetItemValue<long>(messageCountKey);
		}

		public async Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty)
		{
			await ClientPipe.ExecutePipelineAsync(new ClientPipeContextAction((channel, _) =>
			{
				channel.QueueDelete(queueName, ifUnused, ifEmpty);
				return Task.CompletedTask;
			}), _actionPipeline.Value);
		}

		public async Task EnqueueMessageAsync(string queueName, object message)
		{
			await ClientPipe.ExecutePipelineAsync(new ClientPipeContextMessage(message) {ExchangeName = RabbitMqConstants.DefaultExchangeName, RoutingKey = queueName}, _messagePipeline.Value);
		}

		public async Task EnqueueMessageToExchangeAsync(string exchangeName, string routingKey, object message)
		{
			await ClientPipe.ExecutePipelineAsync(new ClientPipeContextMessage(message) {ExchangeName = exchangeName, RoutingKey = routingKey}, _messagePipeline.Value);
		}

		public async Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, ConsumerOptions<T> consumerOptions, Func<T, Dictionary<string, object>, Task> messageProcessor) where T : class
		{
			var consumerPipeline = consumerOptions.BuildPipeline();

			const string consumerTagKey = "consumerTag";
			var pipeContext = new ClientPipeContextAction((channel, context) =>
			{
				var consumerTag = BeginConsumeQueue(channel, queueName, consumerPipeline, messageProcessor);
				context.Items[consumerTagKey] = consumerTag;
				return Task.CompletedTask;
			});
			await ClientPipe.ExecutePipelineAsync(pipeContext, _consumerPipeline.Value);

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

		public RabbitMqEnqueueQueueClient CreateQueueClient(string exchangeName, string routingKey)
		{
			var queueClientPipe = new List<IClientPipe>(_messagePipeline.Value.Span.ToArray());
			queueClientPipe.Insert(0, new QueueSelectionClientPipe(exchangeName, routingKey));
			return new RabbitMqEnqueueQueueClient(new ReadOnlyMemory<IClientPipe>(queueClientPipe.ToArray()));
		}

		public RabbitMqEnqueueQueueClient CreateQueueClient(string queueName)
		{
			var queueClientPipe = new List<IClientPipe>(_messagePipeline.Value.Span.ToArray());
			queueClientPipe.Insert(0, new QueueSelectionClientPipe(queueName));
			return new RabbitMqEnqueueQueueClient(new ReadOnlyMemory<IClientPipe>(queueClientPipe.ToArray()));
		}
	}
}
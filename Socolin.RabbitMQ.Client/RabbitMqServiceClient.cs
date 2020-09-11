using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Options;
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
		Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, Func<T, Task> work) where T : class;
		RabbitMqEnqueueQueueClient CreateQueueClient(string queueName);
	}

	public class RabbitMqServiceClient : IRabbitMqServiceClient
	{
		private readonly RabbitMqServiceClientOptions _options;
		private readonly ReadOnlyMemory<IPipe> _messagePipeline;
		private readonly ReadOnlyMemory<IPipe> _actionPipeline;
		private readonly ReadOnlyMemory<IPipe> _consumerPipeline;

		public RabbitMqServiceClient(RabbitMqServiceClientOptions options)
		{
			_options = options;
			_messagePipeline = options.BuildMessagePipeline();
			_actionPipeline = options.BuildActionPipeline();
			_consumerPipeline = options.BuildConsumerPipeline();
		}

		public async Task CreateQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueueDeclare(queueName, durable, exclusive, autoDelete, arguments);
				return Task.CompletedTask;
			}), _actionPipeline);
		}

		public async Task PurgeQueueAsync(string queueName)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueuePurge(queueName);
				return Task.CompletedTask;
			}), _actionPipeline);
		}

		public async Task<long> GetMessageCountInQueue(string queueName)
		{
			var messageCount = -1L;

			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				try
				{
					messageCount = channel.QueueDeclarePassive(queueName).MessageCount;
				}
				catch (OperationInterruptedException ex) when (ex.ShutdownReason.ReplyCode == 404)
				{
					messageCount = -1;
				}

				return Task.CompletedTask;
			}), _actionPipeline);

			return messageCount;
		}

		public async Task DeleteQueueAsync(string queueName, bool ifUnused, bool ifEmpty)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextAction((channel, _) =>
			{
				channel.QueueDelete(queueName, ifUnused, ifEmpty);
				return Task.CompletedTask;
			}), _actionPipeline);
		}

		public async Task EnqueueMessageAsync(string queueName, object message)
		{
			await Pipe.ExecutePipelineAsync(new PipeContextMessage(message) {QueueName = queueName}, _messagePipeline);
		}

		public async Task<ActiveConsumer> StartListeningQueueAsync<T>(string queueName, Func<T, Task> work) where T : class
		{
			if (_options.Deserialization == null)
				throw new InvalidRabbitMqOptionException("Please provide a deserializer in options before listening to a queue");

			const string consumerTagKey = "consumerTag";
			var pipeContext = new PipeContextAction((channel, context) =>
			{
				var consumerTag = BeginConsumeQueue(channel, queueName, work);
				context.Items[consumerTagKey] = consumerTag;
				return Task.CompletedTask;
			});
			await Pipe.ExecutePipelineAsync(pipeContext, _consumerPipeline);

			return new ActiveConsumer(pipeContext.GetItemValue<string>(consumerTagKey), pipeContext.ChannelContainer!);
		}

		private string BeginConsumeQueue<T>(IModel channel, string queueName, Func<T, Task> work) where T : class
		{
			var consumer = new AsyncEventingBasicConsumer(channel);
			consumer.Received += async (_, message) =>
			{
				object deserialized;
				if (_options.Deserialization!.Deserializers.ContainsKey(message.BasicProperties.ContentType))
					deserialized = _options.Deserialization.Deserializers[message.BasicProperties.ContentType](typeof(T), message.Body);
				else
					deserialized = _options.Deserialization.DefaultDeserializer(typeof(T), message.Body);

				try
				{
					await work((T) deserialized);
					channel.BasicAck(message.DeliveryTag, false);
				}
				catch
				{
					// FIXME: Retry logic (use pipe pattern ?)
					// Make this configurable
					// Requeue message immediatly ?
					// Delay ?
				}
			};

			return channel.BasicConsume(queueName, false, consumer);
		}

		public RabbitMqEnqueueQueueClient CreateQueueClient(string queueName)
		{
			var queueClientPipe = new List<IPipe>(_messagePipeline.Span.ToArray());
			queueClientPipe.Insert(0, new QueueSelectionPipe(queueName));
			return new RabbitMqEnqueueQueueClient(new ReadOnlyMemory<IPipe>(queueClientPipe.ToArray()));
		}
	}
}
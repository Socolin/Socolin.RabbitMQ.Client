using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Newtonsoft.Json;
using NUnit.Framework;
using RabbitMQ.Client.Exceptions;
using Socolin.RabbitMQ.Client.Options;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class RabbitMqServiceClientTests
	{
		private static readonly string BaseQueueName = $"Queue-{nameof(RabbitMqServiceClientTests)}";

		private string _queueName;
		private RabbitMqServiceClient _serviceClient;
		private RabbitMqConnectionManager _rabbitMqConnectionManager;

		[SetUp]
		public void Setup()
		{
			_rabbitMqConnectionManager = new RabbitMqConnectionManager(InitRabbitMqDocker.RabbitMqUri, nameof(RabbitMqServiceClientTests), TimeSpan.FromSeconds(20));

			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(_rabbitMqConnectionManager)
				.WithSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			_serviceClient = new RabbitMqServiceClient(options);
			_queueName = BaseQueueName + Guid.NewGuid();
		}

		[TearDown]
		public async Task TearDown()
		{
			if (_queueName != null)
			{
				try
				{
					using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
					channelContainer.Channel.QueueDelete(_queueName, false, false);
				}
				catch (Exception)
				{
					// Ignored
				}
			}

			_rabbitMqConnectionManager.Dispose();
		}

		[Test]
		public async Task CanCreateQueue_ThenEnqueue()
		{
			var randomMessage = Guid.NewGuid();

			await _serviceClient.CreateQueueAsync(_queueName);
			await _serviceClient.EnqueueMessageAsync(_queueName, new {test = randomMessage});

			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			var message = channelContainer.Channel.BasicGet(_queueName, true);
			Encoding.UTF8.GetString(message.Body.Span).Should().BeEquivalentTo($"{{\"test\":\"{randomMessage}\"}}");
		}

		[Test]
		public async Task CanCreateQueue_ThenEnqueueWithQueueClient()
		{
			var randomMessage = Guid.NewGuid();

			await _serviceClient.CreateQueueAsync(_queueName);
			await _serviceClient.CreateQueueClient(_queueName).EnqueueMessageAsync(new {test = randomMessage});

			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			var message = channelContainer.Channel.BasicGet(_queueName, true);
			Encoding.UTF8.GetString(message.Body.Span).Should().BeEquivalentTo($"{{\"test\":\"{randomMessage}\"}}");
		}

		[Test]
		public async Task CanPurgeQueue()
		{
			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			channelContainer.Channel.QueueDeclare(_queueName, true, false, false, null);
			channelContainer.Channel.BasicPublish("", _queueName, true, null, new byte[] {0x42});

			await _serviceClient.PurgeQueueAsync(_queueName);

			var message = channelContainer.Channel.BasicGet(_queueName, true);
			message.Should().BeNull();
		}

		[Test]
		public async Task CanDeleteQueue()
		{
			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			channelContainer.Channel.QueueDeclare(_queueName, true, false, false, null);
			channelContainer.Channel.BasicPublish("", _queueName, true, null, new byte[] {0x42});

			await _serviceClient.DeleteQueueAsync(_queueName, false, false);

			// ReSharper disable once AccessToDisposedClosure
			Action assert = () => channelContainer.Channel.QueuePurge(_queueName);
			assert.Should().Throw<OperationInterruptedException>().Which.ShutdownReason.ReplyCode.Should().Be(404);
		}

		[Test]
		public async Task CanGetMessageCount()
		{
			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			channelContainer.Channel.QueueDeclare(_queueName, true, false, false, null);
			channelContainer.Channel.BasicPublish("", _queueName, true, null, new byte[] {0x42});
			channelContainer.Channel.BasicPublish("", _queueName, true, null, new byte[] {0x42});

			var count = await _serviceClient.GetMessageCountInQueueAsync(_queueName);

			count.Should().Be(2);
		}

		[Test]
		public async Task CanGetMessageCount_OnNonExistentQueue()
		{
			var count = await _serviceClient.GetMessageCountInQueueAsync(_queueName);
			count.Should().Be(-1);
		}

		[Test]
		[Explicit]
		public async Task ClientSurviveRestart()
		{
			var randomMessage1 = Guid.NewGuid();
			var randomMessage2 = Guid.NewGuid();
			await _serviceClient.CreateQueueAsync(_queueName);

			await _serviceClient.EnqueueMessageAsync(_queueName, new {test = "first-" + randomMessage1});
			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel();
			var message1 = Encoding.UTF8.GetString(channelContainer.Channel.BasicGet(_queueName, true).Body.Span);

			InitRabbitMqDocker.RestartRabbitMq();

			await _serviceClient.EnqueueMessageAsync(_queueName, new {test = "second-" + randomMessage2});
			using var channelContainer2 = await _rabbitMqConnectionManager.AcquireChannel();
			var message2 = Encoding.UTF8.GetString(channelContainer2.Channel.BasicGet(_queueName, true).Body.Span);

			using (new AssertionScope())
			{
				message1.Should().BeEquivalentTo($"{{\"test\":\"first-{randomMessage1}\"}}");
				message2.Should().BeEquivalentTo($"{{\"test\":\"second-{randomMessage2}\"}}");
			}
		}

		[Test]
		public async Task CanListenToMessage()
		{
			var randomMessage = Guid.NewGuid();
			var semaphore = new SemaphoreSlim(1);

			await _serviceClient.CreateQueueAsync(_queueName);

			var actualMessages = new List<string>();
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.Build();

			await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _) =>
			{
				actualMessages.Add(message);
				semaphore.Release();
				return Task.CompletedTask;
			});

			await semaphore.WaitAsync();
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(10));

			actualMessages.Should().BeEquivalentTo(randomMessage.ToString());
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}

		[Test]
		[Explicit]
		public async Task CanListenToMessageAndSurviveRestart()
		{
			var randomMessage1 = "first-" + Guid.NewGuid();
			var randomMessage2 = "second-" + Guid.NewGuid();
			var semaphore = new SemaphoreSlim(1);

			await _serviceClient.CreateQueueAsync(_queueName);

			var actualMessages = new List<string>();
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.Build();
			await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _) =>
			{
				actualMessages.Add(message);
				semaphore.Release();
				return Task.CompletedTask;
			});

			await semaphore.WaitAsync();
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage1);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(20));
			InitRabbitMqDocker.RestartRabbitMq();

			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage2);

			await semaphore.WaitAsync(TimeSpan.FromSeconds(20));
			actualMessages.Should().BeEquivalentTo(randomMessage1, randomMessage2);
		}

		[Test]
		public async Task CanListenToMessageAndCancelIt()
		{
			var randomMessage = Guid.NewGuid();
			var semaphore = new SemaphoreSlim(1);

			await _serviceClient.CreateQueueAsync(_queueName);

			var actualMessages = new List<string>();
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.Build();
			var activeConsumer = await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _) =>
			{
				actualMessages.Add(message);
				semaphore.Release();
				return Task.CompletedTask;
			});

			await semaphore.WaitAsync();
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(20));
			activeConsumer.Cancel();
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

			actualMessages.Should().HaveCount(1);
		}


		[Test]
		public async Task CanListenToMultipleMessages()
		{
			var randomMessage1 = Guid.NewGuid().ToString();
			var randomMessage2 = Guid.NewGuid().ToString();
			var randomMessage3 = Guid.NewGuid().ToString();
			var semaphore = new SemaphoreSlim(1);

			await _serviceClient.CreateQueueAsync(_queueName);

			var actualMessages = new List<string>();
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithSimpleMessageAck()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.Build();

			await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _) =>
			{
				actualMessages.Add(message);
				semaphore.Release();
				return Task.CompletedTask;
			});

			await semaphore.WaitAsync();
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage1);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(10));
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage2);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(1));
			await _serviceClient.EnqueueMessageAsync(_queueName, randomMessage3);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(1));

			actualMessages.Should().BeEquivalentTo(randomMessage1, randomMessage2, randomMessage3);
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}

	}
}
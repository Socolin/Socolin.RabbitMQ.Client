using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class DelayedRetryTests
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
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			_serviceClient = new RabbitMqServiceClient(options);
			_queueName = BaseQueueName + Guid.NewGuid();
			_serviceClient.CreateQueueAsync(_queueName);
		}

		[TearDown]
		public async Task TearDown()
		{
			if (_queueName != null)
			{
				try
				{
					using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel(ChannelType.Publish);
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
		public async Task CanUseDelayedConsumer()
		{
			var actualMessages = new List<string>();
			var semaphore = new SemaphoreSlim(1);
			var delayQueueName = await DelayedRetryMessageAcknowledgementPipe<string>.CreateDelayQueueAsync(_serviceClient, _queueName);
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithDelayedRetryMessageAck(new[] {TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)})
				.Build();

			await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _, ct) =>
			{
				actualMessages.Add(message);
				semaphore.Release();
				return Task.FromException(new Exception("test"));
			});

			var sw = Stopwatch.StartNew();
			await semaphore.WaitAsync();
			(await _serviceClient.GetMessageCountInQueueAsync(delayQueueName)).Should().Be(0);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await semaphore.WaitAsync(TimeSpan.FromSeconds(2));
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			(await _serviceClient.GetMessageCountInQueueAsync(delayQueueName)).Should().Be(1);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(2));
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			(await _serviceClient.GetMessageCountInQueueAsync(delayQueueName)).Should().Be(1);
			await semaphore.WaitAsync(TimeSpan.FromSeconds(2));
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			(await _serviceClient.GetMessageCountInQueueAsync(delayQueueName)).Should().Be(0);

			sw.ElapsedMilliseconds.Should().BeGreaterThan(3000);
			actualMessages.Should().HaveCount(3);
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}
	}
}
using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class CancelActiveConsumerTests
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
		public async Task CanCancelAndWaitCurrentTaskToBeCompleted()
		{
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.Build();

			var completed = false;
			var messageCount = 0;
			var activeConsumer = await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, async (message, _, ct) =>
			{
				messageCount++;
				await Task.Delay(TimeSpan.FromSeconds(1), ct);
				completed = true;
			});

			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await Task.Delay(TimeSpan.FromMilliseconds(200));

			await activeConsumer.CancelAfterCurrentTaskCompletedAsync();

			messageCount.Should().Be(1);
			completed.Should().Be(true);
		}


		[Test]
		public async Task CanCancelAndInterruptCurrentProcessing()
		{
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.Build();

			var completed = false;
			var messageCount = 0;
			var activeConsumer = await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, async (message, _, ct) =>
			{
				messageCount++;
				await Task.Delay(TimeSpan.FromSeconds(1), ct);
				completed = true;
			});

			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await Task.Delay(TimeSpan.FromMilliseconds(200));

			await activeConsumer.CancelAsync();

			messageCount.Should().Be(1);
			completed.Should().Be(false);
		}
	}
}
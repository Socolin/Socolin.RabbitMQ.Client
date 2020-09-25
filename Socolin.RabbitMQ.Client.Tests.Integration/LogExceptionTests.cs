using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class LogExceptionTests
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
			var thrownException = new Exception();
			var semaphore = new SemaphoreSlim(1);
			var receivedExceptions = new List<(Exception exception, bool lastAttempt)>();
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithFastRetryMessageAck(3)
				.WithErrorLogging((exception, lastAttempt) => receivedExceptions.Add((exception, lastAttempt)))
				.Build();

			await _serviceClient.StartListeningQueueAsync(_queueName, consumerOptions, (message, _, ct) =>
			{
				semaphore.Release();
				return Task.FromException(thrownException);
			});

			await semaphore.WaitAsync();
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await semaphore.WaitAsync();
			await Task.Delay(TimeSpan.FromSeconds(1));

			receivedExceptions.Should().BeEquivalentTo(
				(thrownException, false),
				(thrownException, false),
				(thrownException, false),
				(thrownException, true)
			);
		}
	}
}
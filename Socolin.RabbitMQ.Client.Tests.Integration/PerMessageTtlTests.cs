using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Client;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class PerMessageTtlTests
	{
		private static readonly string BaseQueueName = $"Queue-{nameof(RabbitMqServiceClientTests)}";

		private string _queueName;
		private RabbitMqConnectionManager _rabbitMqConnectionManager;

		[SetUp]
		public void Setup()
		{
			_rabbitMqConnectionManager = new RabbitMqConnectionManager(InitRabbitMqDocker.RabbitMqUri, nameof(RabbitMqServiceClientTests), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(60));
			_queueName = BaseQueueName + Guid.NewGuid();
		}

		[TearDown]
		public async Task TearDown()
		{
			if (_queueName != null)
			{
				try
				{
					using var channelContainer = await _rabbitMqConnectionManager.AcquireChannelAsync(ChannelType.Publish);
					await channelContainer.Channel.QueueDeleteAsync(_queueName, false, false);
				}
				catch (Exception)
				{
					// Ignored
				}
			}

			_rabbitMqConnectionManager.Dispose();
		}

		[Test]
		public async Task CanUsePerMessageTtl()
		{
			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(_rabbitMqConnectionManager)
				.WithPerMessageTtl(500)
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			var serviceClient = new RabbitMqServiceClient(options);

			await serviceClient.CreateQueueAsync(_queueName);
			await serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await Task.Delay(TimeSpan.FromMilliseconds(600));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}

		[Test]
		public async Task CanConfigureWhenEnqueue()
		{
			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(_rabbitMqConnectionManager)
				.WithPerMessageTtl()
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			var serviceClient = new RabbitMqServiceClient(options);

			await serviceClient.CreateQueueAsync(_queueName);
			await serviceClient.EnqueueMessageAsync(_queueName, "some-message", new Dictionary<string, object>
			{
				[MessageTtlClientPipe.ContextItemExpirationKey] = 300
			});
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await Task.Delay(TimeSpan.FromMilliseconds(400));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}

		[Test]
		public async Task CanOverridePerMessageTtl()
		{
			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(_rabbitMqConnectionManager)
				.WithPerMessageTtl(500)
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			var serviceClient = new RabbitMqServiceClient(options);

			await serviceClient.CreateQueueAsync(_queueName);
			await serviceClient.EnqueueMessageAsync(_queueName, "some-message", new Dictionary<string, object>
			{
				[MessageTtlClientPipe.ContextItemExpirationKey] = 1_000
			});
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await Task.Delay(TimeSpan.FromMilliseconds(600));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await Task.Delay(TimeSpan.FromMilliseconds(600));
			(await serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}
	}
}
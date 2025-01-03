using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class QueueOptionsTests
	{
		private static readonly string BaseQueueName = $"Queue-{nameof(QueueOptionsTests)}";
		private RabbitMqConnectionManager _rabbitMqConnectionManager;
		private string _queueName;
		private string _errorQueueName;
		private RabbitMqServiceClient _serviceClient;
		private ConsumerOptions<string> _consumerOptions;

		[SetUp]
		public void Setup()
		{
			_rabbitMqConnectionManager = new RabbitMqConnectionManager(InitRabbitMqDocker.RabbitMqUri, nameof(RabbitMqServiceClientTests), TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(60));

			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(_rabbitMqConnectionManager)
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			_serviceClient = new RabbitMqServiceClient(options);
			_queueName = BaseQueueName + Guid.NewGuid();
			_errorQueueName = _queueName + "-Error";

			_consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithSimpleMessageAck()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.Build();
		}

		[TearDown]
		public async Task TearDown()
		{
			await _serviceClient.DeleteQueueAsync(_queueName, false, false);
		}

		[Test]
		public async Task CanCreateAutoDeleteQueue()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.AutoDelete()
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			var activeConsumer = await _serviceClient.StartListeningQueueAsync(_queueName, _consumerOptions, (_, _, _) => Task.CompletedTask);
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			await activeConsumer.CancelAsync();
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(-1);
		}

		[Test]
		public async Task CanCreateWithExpires()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithExpire(100)
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			await Task.Delay(TimeSpan.FromMilliseconds(300));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(-1);
		}

		[Test]
		public async Task CanCreateQueue_WithLazyMode()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithLazyMode()
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			// FIXME: Is there a way to test this ?
		}

		[Test]
		public async Task CanCreateQueue_WithDeadLetterQueue()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithDeadLetterExchange(RabbitMqConstants.DefaultExchangeName)
				.WithDeadLetterRoutingKey(_errorQueueName)
				.Build();

			await _serviceClient.CreateQueueAsync(_errorQueueName, false);

			await _serviceClient.CreateQueueAsync(_queueName, options);
			var activeConsumer = await _serviceClient.StartListeningQueueAsync(_queueName, _consumerOptions, (_, _, _) => Task.FromException(new Exception("Test")));
			(await _serviceClient.GetMessageCountInQueueAsync(_errorQueueName)).Should().Be(0);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");

			await Task.Delay(TimeSpan.FromMilliseconds(100));

			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
			(await _serviceClient.GetMessageCountInQueueAsync(_errorQueueName)).Should().Be(1);
			await activeConsumer.CancelAsync();
		}

		[Test]
		public async Task CanCreateQueue_WithTtl()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithMessageTtl(100)
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");
			await Task.Delay(TimeSpan.FromMilliseconds(50));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await Task.Delay(TimeSpan.FromMilliseconds(200));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(0);
		}


		[Test]
		public async Task CanCreateQueue_WithMaxLengthAndOverflowModeRejectPublish()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithMaxLength(1)
				.WithOverflowBehaviour(OverflowBehaviour.RejectPublish)
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-1");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-2");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannelAsync(ChannelType.Consumer);
			var message = Encoding.UTF8.GetString((await channelContainer.Channel.BasicGetAsync(_queueName, true))!.Body.Span);
			message.Should().Be("\"some-message-1\"");
		}

		[Test]
		public async Task CanCreateQueue_WithMaxLengthAndOverflowModeDropHead()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithMaxLengthByte(16)
				.WithOverflowBehaviour(OverflowBehaviour.DropHead)
				.Build();

			await _serviceClient.CreateQueueAsync(_queueName, options);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-1");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-2");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);

			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannelAsync(ChannelType.Consumer);
			var message = Encoding.UTF8.GetString((await channelContainer.Channel.BasicGetAsync(_queueName, true))!.Body.Span);
			message.Should().Be("\"some-message-2\"");
		}

		[Test]
		public async Task CanCreateQueue_WithMaxLengthAndOverflowModeRejectPublishDlx()
		{
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithDeadLetterExchange(RabbitMqConstants.DefaultExchangeName)
				.WithDeadLetterRoutingKey(_errorQueueName)
				.WithMaxLength(1)
				.WithOverflowBehaviour(OverflowBehaviour.RejectPublishDlx)
				.Build();

			await _serviceClient.CreateQueueAsync(_errorQueueName, false);

			await _serviceClient.CreateQueueAsync(_queueName, options);
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-1");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			(await _serviceClient.GetMessageCountInQueueAsync(_errorQueueName)).Should().Be(0);

			// This message should be rejected and enqueue in the errorQueue
			await _serviceClient.EnqueueMessageAsync(_queueName, "some-message-2");
			await Task.Delay(TimeSpan.FromMilliseconds(100));
			(await _serviceClient.GetMessageCountInQueueAsync(_queueName)).Should().Be(1);
			(await _serviceClient.GetMessageCountInQueueAsync(_errorQueueName)).Should().Be(1);

			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannelAsync(ChannelType.Consumer);
			var message = Encoding.UTF8.GetString((await channelContainer.Channel.BasicGetAsync(_queueName, true))!.Body.Span);
			message.Should().Be("\"some-message-1\"");
			var messageError = Encoding.UTF8.GetString((await channelContainer.Channel.BasicGetAsync(_errorQueueName, true))!.Body.Span);
			messageError.Should().Be("\"some-message-2\"");
		}

		[Test]
		public async Task CanCreateQueue_WithSingleActiveConsumer()
		{
			var usedConsumerIds = new List<int>();
			var options = new CreateQueueOptionsBuilder(QueueType.Classic)
				.WithSingleActiveConsumer()
				.Build();
			await _serviceClient.CreateQueueAsync(_queueName, options);

			var activeConsumer1 = await _serviceClient.StartListeningQueueAsync(_queueName, _consumerOptions, (_, _, _) =>
			{
				usedConsumerIds.Add(1);
				return Task.CompletedTask;
			});
			var activeConsumer2 = await _serviceClient.StartListeningQueueAsync(_queueName, _consumerOptions, (_, _, _) =>
			{
				usedConsumerIds.Add(2);
				return Task.CompletedTask;
			});

			for (var i = 0; i < 1_000; i++)
				await _serviceClient.EnqueueMessageAsync(_queueName, "some-message");

			var sw = Stopwatch.StartNew();
			while (usedConsumerIds.Count < 1_000 && sw.ElapsedMilliseconds < 10_000)
				await Task.Delay(TimeSpan.FromMilliseconds(10));

			await activeConsumer1.CancelAsync();
			await activeConsumer2.CancelAsync();
			usedConsumerIds.Should().AllBeEquivalentTo(usedConsumerIds.First());
			usedConsumerIds.Should().HaveCount(1_000);
		}
	}
}
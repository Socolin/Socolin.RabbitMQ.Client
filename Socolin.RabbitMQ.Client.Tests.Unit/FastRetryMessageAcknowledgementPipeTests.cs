using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Socolin.RabbitMQ.Client.Pipes.Consumer;
using Socolin.RabbitMQ.Client.Pipes.Consumer.Context;

namespace Socolin.RabbitMQ.Client.Tests.Unit
{
	public class FastRetryMessageAcknowledgementPipeTests
	{
		private const ulong DeliveryTag = 27972ul;
		private const string RoutingKey = "some-routing-key";
		private static readonly byte[] Body = [0x42, 0x66];

		private FastRetryMessageAcknowledgementPipe<string> _pipe;

		private Mock<IRabbitMqConnectionManager> _rabbitMqConnectionManager;
		private ConsumerPipeContext<string> _consumerPipeContext;
		private Mock<IChannel> _channelMock;
		private Mock<IConsumerPipe<string>> _nextPipeMock;
		private ReadOnlyMemory<IConsumerPipe<string>> _fakePipeline;
		private BasicDeliverEventArgs _basicDeliverEventArgs;
		private BasicProperties _basicProperties;
		private Mock<IChannel> _publishChannelMock;

		[SetUp]
		public void Setup()
		{
			_pipe = new FastRetryMessageAcknowledgementPipe<string>(2);

			_nextPipeMock = new Mock<IConsumerPipe<string>>(MockBehavior.Strict);
			_fakePipeline = new ReadOnlyMemory<IConsumerPipe<string>>([_nextPipeMock.Object]);
			_basicProperties = new BasicProperties();

			_basicDeliverEventArgs = new BasicDeliverEventArgs(
				"some-consumer-tag",
				DeliveryTag,
				false,
				"some-exchange",
				RoutingKey,
				_basicProperties,
				Body
			);
			_rabbitMqConnectionManager = new Mock<IRabbitMqConnectionManager>(MockBehavior.Strict);
			_channelMock = new Mock<IChannel>(MockBehavior.Strict);
			_consumerPipeContext = new ConsumerPipeContext<string>(_rabbitMqConnectionManager.Object, _channelMock.Object, _basicDeliverEventArgs, (_, _, _) => Task.CompletedTask, Mock.Of<IActiveMessageProcessorCanceller>());

			_publishChannelMock = new Mock<IChannel>(MockBehavior.Strict);
			_rabbitMqConnectionManager.Setup(m => m.AcquireChannelAsync(ChannelType.Publish))
				.ReturnsAsync(() => new ChannelContainer(Mock.Of<IRabbitMqChannelManager>(), _publishChannelMock.Object));
		}

		[Test]
		public async Task ProcessAsync_WhenItWorks_AckTheMessage()
		{
			_channelMock.Setup(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask)
				.Verifiable();
			_nextPipeMock.Setup(m => m.ProcessAsync(_consumerPipeContext, It.IsAny<ReadOnlyMemory<IConsumerPipe<string>>>(), It.IsAny<CancellationToken>()))
				.Returns(Task.CompletedTask);

			await _pipe.ProcessAsync(_consumerPipeContext, _fakePipeline);

			_channelMock.Verify(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Test]
		public async Task ProcessAsync_WhenProcessFail_AddHeaderOnMessageAndRepublishIt_AndAcknowledgeCurrentMessage()
		{
			var s = new MockSequence();
			_publishChannelMock.InSequence(s).Setup(m => m.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, RoutingKey, true, It.Is<BasicProperties>(x => (int)x.Headers["RetryCount"] == 1), Body, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask);
			_channelMock.InSequence(s).Setup(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask);

			_nextPipeMock.Setup(m => m.ProcessAsync(_consumerPipeContext, It.IsAny<ReadOnlyMemory<IConsumerPipe<string>>>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromException(new Exception("forced")));

			await _pipe.ProcessAsync(_consumerPipeContext, _fakePipeline);

			_publishChannelMock.Verify(m => m.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, RoutingKey, true, It.Is<BasicProperties>(x => (int)x.Headers["RetryCount"] == 1), Body, It.IsAny<CancellationToken>()), Times.Once);
			_channelMock.Verify(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
		}


		[Test]
		public async Task ProcessAsync_WhenProcessFail_IncrementRetryCount_AndRepublishMessageAndAckCurrentMessage()
		{
			var s = new MockSequence();
			_publishChannelMock.InSequence(s).Setup(m => m.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, RoutingKey, true, It.Is<BasicProperties>(x => (int)x.Headers["RetryCount"] == 2), Body, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask);
			_channelMock.InSequence(s).Setup(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask);

			_basicProperties.Headers = new Dictionary<string, object> {["RetryCount"] = 1};
			_nextPipeMock.Setup(m => m.ProcessAsync(_consumerPipeContext, It.IsAny<ReadOnlyMemory<IConsumerPipe<string>>>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromException(new Exception("forced")));

			await _pipe.ProcessAsync(_consumerPipeContext, _fakePipeline);

			_publishChannelMock.Verify(m => m.BasicPublishAsync(RabbitMqConstants.DefaultExchangeName, RoutingKey, true, It.Is<BasicProperties>(x => (int)x.Headers["RetryCount"] == 2), Body, It.IsAny<CancellationToken>()), Times.Once);
			_channelMock.Verify(m => m.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
		}

		[Test]
		public async Task ProcessAsync_WhenProcessFail_RejectMessageAfterTooManyRetry()
		{
			_channelMock.Setup(m => m.BasicRejectAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
				.Returns(ValueTask.CompletedTask);

			_basicProperties.Headers = new Dictionary<string, object> {["RetryCount"] = 2};
			_nextPipeMock.Setup(m => m.ProcessAsync(_consumerPipeContext, It.IsAny<ReadOnlyMemory<IConsumerPipe<string>>>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromException(new Exception("forced")));

			await _pipe.ProcessAsync(_consumerPipeContext, _fakePipeline);

			_channelMock.Verify(m => m.BasicRejectAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
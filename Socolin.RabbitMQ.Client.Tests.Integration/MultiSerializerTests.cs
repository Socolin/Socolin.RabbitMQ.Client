using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	public class MultiSerializerTests
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
				.WithSerializer(BsonSerializer, "application/bson")
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
		public async Task CanEnqueueMessageUsingJsonOrBsonSerializer()
		{
			await _serviceClient.CreateQueueAsync(_queueName);
			await _serviceClient.EnqueueMessageAsync(_queueName, new {test = "test1"}, "application/json");
			await _serviceClient.EnqueueMessageAsync(_queueName, new {test = "test2"}, "application/bson");
			await Task.Delay(TimeSpan.FromMilliseconds(100));

			using var channelContainer = await _rabbitMqConnectionManager.AcquireChannel(ChannelType.Consumer);
			var message1 = channelContainer.Channel.BasicGet(_queueName, true);
			message1.BasicProperties.ContentType.Should().Be("application/json");
			Encoding.UTF8.GetString(message1.Body.Span).Should().BeEquivalentTo("{\"test\":\"test1\"}");

			var message2 = channelContainer.Channel.BasicGet(_queueName, true);
			message2.BasicProperties.ContentType.Should().Be("application/bson");
			message2.Body.ToArray().Should().BeEquivalentTo(new byte[]
			{
				0x15, 0x00, 0x00, 0x00, 0x02, 0x74, 0x65, 0x73, 0x74, 0x00, 0x06, 0x00, 0x00, 0x00, 0x74, 0x65, 0x73, 0x74, 0x32, 0x00, 0x00
			});
		}

		byte[] BsonSerializer(object message)
		{
			var ms = new MemoryStream();
			using var bsonWriter = new BsonDataWriter(ms);
			var serializer = new JsonSerializer();
			serializer.Serialize(bsonWriter, message);
			return new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int) ms.Position).ToArray();
		}
	}
}
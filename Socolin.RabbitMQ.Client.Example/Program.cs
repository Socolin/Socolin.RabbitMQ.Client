using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Options.Consumer;

namespace Socolin.RabbitMQ.Client.Example
{
	public class Program
	{
		public static async Task Main()
		{
			var rabbitMqConnectionManager = new RabbitMqConnectionManager(new Uri("amqp://localhost"), "test", TimeSpan.FromSeconds(30));
			const string queueName = "some-queue-name";
			var options = new RabbitMqServiceOptionsBuilder()
				.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
				.WithConnectionManager(rabbitMqConnectionManager)
				.WithDefaultSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
				.Build();
			var serviceClient = new RabbitMqServiceClient(options);

			// Create a queue
			await serviceClient.CreateQueueAsync(queueName + "-Error", true);

			// Create a queue with options builder
			var createQueueOption = new CreateQueueOptionsBuilder(QueueType.Classic)
				.Durable()
				.WithDeadLetterExchange(RabbitMqConstants.DefaultExchangeName)
				.WithDeadLetterRoutingKey(queueName + "-Error")
				.Build();
			await serviceClient.CreateQueueAsync(queueName, createQueueOption);

			// Listen to queue (Auto reconnect is enabled)
			var consumerOptions = new ConsumerOptionsBuilder<string>()
				.WithDefaultDeSerializer(message => JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(message.Span)))
				.WithSimpleMessageAck()
				.WithCustomPipe(async (context, next) =>
				{
					Console.WriteLine("Some logging message before processing");
					await next();
					Console.WriteLine("Some logging message after processing");
				})
				.Build();
			var activeConsumer = await serviceClient.StartListeningQueueAsync(queueName, consumerOptions, (message, items, ct) =>
			{
				Console.WriteLine(message);
				return Task.CompletedTask;
			});

			// Enqueue a message
			await serviceClient.EnqueueMessageAsync(queueName, "some-message");
			await Task.Delay(100);

			// Enqueue using EnqueueQueueClient
			var queueClient = serviceClient.CreateQueueClient(queueName);
			await queueClient.EnqueueMessageAsync("some-other-message");

			// Cancel listening
			activeConsumer.Cancel();

			// Purge the queue
			await serviceClient.PurgeQueueAsync(queueName);

			// Delete a queue
			await serviceClient.DeleteQueueAsync(queueName, false, false);
			await serviceClient.DeleteQueueAsync(queueName + "-Error", false, false);
		}
	}
}
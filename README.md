# Socolin.RabbitMQ.Client

A simple wrapper around [RabbitMQ.Client](https://github.com/rabbitmq/rabbitmq-dotnet-client) to make it easier to use.

## Example

```cs
const string queueName = "some-queue-name";
var options = new RabbitMqServiceOptionsBuilder()
	.WithRetry(TimeSpan.FromSeconds(15), null, TimeSpan.FromSeconds(1))
	.WithConnectionManager(_rabbitMqConnectionManager)
	.WithDefaultDeSerializer((type, message) => JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Span), type))
	.WithSerializer(message => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)), "application/json")
	.Build();
var serviceClient = new RabbitMqServiceClient(options);

// Create a queue
await serviceClient.CreateQueueAsync(queueName);

// Listen to queue (Auto reconnect is enabled)
var activeConsumer =  await serviceClient.StartListeningQueueAsync<string>(queueName, message =>
{
    Console.WriteLine(message);
    return Task.CompletedTask;
});

// Enqueue a message
await serviceClient.EnqueueMessageAsync(queueName, "some-message");

// Enqueue using EnqueueQueueClient
var queueClient = serviceClient.CreateQueueClient(_queueName);
await queueClient.EnqueueMessageAsync("some-other-message");

// Cancel listening
activeConsumer.Cancel();
```

## Customization

Internally the library is using pipeline pattern.

- When publishing a message it's using _Message Pipeline_
- When executing an action (Create/Delete/Purge a queue or start consuming it) it's using the _Action Pipeline_

Retry logic can be rewrite by replacing the `Retry` pipe using `.WithRetry()` on the `RabbitMqServiceOptionsBuilder`
Custom pipes can be inserted in the pipes using `.WithCustomPipe()` or directly in `RabbitMqServiceOptions.Customs`
- `IGenericPipe` `IMessagePipe` will be inserted in _Message Pipeline_
- `IGenericPipe` `IActionPipe` will be inserted in _Action Pipeline_

### Message Pipeline

![Message Pipeline](./doc/images/message-pipeline.png)

### Action Pipeline

![Action Pipeline](./doc/images/action-pipeline.png)


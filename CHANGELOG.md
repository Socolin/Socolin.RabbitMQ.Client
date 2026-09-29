## 2.2.0 - 2026-09-29

- Add optional `EnqueueMessageOptions` parameters to queue, exchange, and reusable queue client enqueue APIs
- Add nullable per-message priority support with values from 0 to 9
- Update `RabbitMQ.Client` from 7.0.0 to 7.2.2 and `JetBrains.Annotations` from 2024.3.0 to 2026.2.0
- Update example and test NuGet packages

## 2.1.4 - 2026-01-23

- Fix typo, fix #2

## 2.1.3 - 2026-01-21

- Add a way to access the cancellation token in custom pipe

## 2.1.0 - 2025-12-05

- Add setting to configure RequestedChannelMax on ConnectionFactory

## 2.0.1 - 2025-01-07

- Fix "Too many channel allocated"

## 2.0.0 - 2025-01-03

- Convert to RabbitMQ.Client
- Update for RabbitMq.Client v7
- Convert to file-scoped namespace

## 1.7.9 - 2024-09-13

- Add method to get the number of consumers

## 1.7.8 - 2021-12-27

- Fix potential connection leak

## 1.7.7 - 2021-02-04

- Expose `GetMessageCountInQueueAsync` API

## 1.7.6 - 2020-12-19

- Test about sourcelink

## 1.7.5 - 2020-12-15

- Test about sourcelink

## 1.7.4 - 2020-12-15

- Enable [sourcelink](https://github.com/dotnet/sourcelink)

## 1.7.3 - 2020-12-08

- Add option to allow to override `mandatory` flag when publishing a message
- Add option to configure connection heartbeat (disabled by default)

## 1.7.2 - 2020-09-25

- Add new consumer option to choose consumer prefetch count

## 1.7.1 - 2020-09-24

- Rework Consumer pipeline. Since deserialization can fail, it has been move after the _Log Exception Pipe_ and after the _Message Ack Pipe_, so it does not "lock" the consumer when deserializer fail.

## 1.7.0 - 2020-09-24

- Follow [RabbitMQ recommendations](https://www.cloudamqp.com/blog/2018-01-19-part4-rabbitmq-13-common-errors.html) about connections
    - 1 connection for publishing
    - 1 connection for consuming
- When using `.WithSerializer()` define default deserializer with it if none has been defined

## 1.6.0 - 2020-09-24

- `StartListeningQueueAsync` delegate will now take CancellationToken as 3rd argument.
- Add new methods on `IActiveConsumer` to choose how to cancel the current processing
    - `CancelAfterCurrentTaskCompletedAsync()` will prevent further processing after the current processing message has been completed.
    - `CancelAsync()` will cancel the token given to the `StartListeningQueueAsync` and cancel further processing.

## 1.5.2 - 2020-09-22

- Fix potential crash when no Content-Type has been provided in message properties

## 1.5.1 - 2020-09-22

- default deserializer is now optional if another deserializer has been specified

## 1.5.0 - 2020-09-21

- Add a new pipe to log processing exception in consumer pipe: `LogExceptionConsumerPipe`
- Add a way to configure DeliveryMode on message `RabbitMqServiceOptionsBuilder.WithDeliveryMode()`

## 1.4.0 - 2020-09-19

- Declare delegates for serializer/deserializer functions
- Rework Serialization logic to allow specify which serializer to use when enqueueing a message

## 1.3.2 - 2020-09-18

- Declare delegate to improve readability and usage of `messageProcessor` in  `StartListeningQueueAsync`

## 1.3.1 - 2020-09-17

- Add a new overload `WithCustomPipe` on `ConsumerOptionsBuilder` to be able to define pipeline inline. (Like `.Use()` for ASP.NET Core middlewares)

## 1.3.0 - 2020-09-17

- Allow specifying an array of TimeSpan for client retry logic.

## 1.2.1 - 2020-09-14

- Fix potential issue with retry logic

## 1.2.0 - 2020-09-13

- Allow specifying ExchangeName/RoutingKey when publishing a message as an alternative to QueueName.
- Add new `CreateQueueOptionBuilder` to simplify how to create a queue with custom arguments.
- Allow specifying `contextItems` when enqueueing a message. This databag is available through the `IPipeContext.Items` in pipes.
- Add a new option to configure message expiration
- Add new _Consumer Pipe_ `DelayedRetryMessageAcknowledgementPipe` to retry failed message with a delay between retry

## 1.1.0 - 2020-09-12

- Create pipelines only when they are needed. So it does not throw because Serializer is missing when creating a client to listen to a queue, for example.
- Rework how to consume messages from a queue. It now uses pipeline pattern. See documentation for more details.
- Add new _Consumer Pipe_ `FastRetryMessageAcknowledgementPipe` that can handle retry when a message encounters an error during processing.

## 1.0.0 - 2020-09-11

- Initial Release
- Create/Delete/Purge Queue
- Listen to a queue (no error handling yet)
- Publish message
- Retry logic for all action if the server is down

1.2

- Allow specifying ExchangeName/RoutingKey when publishing a message as an alternative to QueueName.
- Add new `CreateQueueOptionBuilder` to simplify how to create a queue with custom arguments.
- Allow specifying `contextItems` when enqueueing a message. This databag is available through the `IPipeContext.Items` in pipes.
- Add a new option to configure message expiration

1.1

- Create pipelines only when they are needed. So it does not throw because Serializer is missing when creating a client to listen to a queue, for example.
- Rework how to consume messages from a queue. It now uses pipeline pattern. See documentation for more details.
- Add new _Consumer Pipe_ `FastRetryMessageAcknowledgementPipe` that can handle retry when a message encounters an error during processing.

1.0

 - Initial Release
 - Create/Delete/Purge Queue
 - Listen to a queue (no error handling yet)
 - Publish message
 - Retry logic for all action if the server is down
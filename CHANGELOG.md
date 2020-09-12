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
1.1

- Create pipelines only when they are needed. So it does not throw because Serializer is missing when creating a client to listen to a queue, for example.

1.0

 - Initial Release
 - Create/Delete/Purge Queue
 - Listen to a queue (no error handling yet)
 - Publish message
 - Retry logic for all action if the server is down
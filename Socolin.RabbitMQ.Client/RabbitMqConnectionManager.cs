using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client
{
	public interface IRabbitMqConnectionManager : IDisposable
	{
		Task<ChannelContainer> AcquireChannel();
		void ReleaseChannel(IModel channel);
	}

	public class RabbitMqConnectionManager : IRabbitMqConnectionManager
	{
		private const int MaxChannelPool = 90;
		private readonly Uri _uri;
		private readonly string _connectionName;
		private readonly TimeSpan _connectionTimeout;

		private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);
		private IConnection? _connection;

		private readonly ConcurrentBag<IModel> _availableChannelPool = new ConcurrentBag<IModel>();
		private readonly object _lockUsedChannelCount = new object();
		private int _usedChannelCount;

		public RabbitMqConnectionManager(Uri uri, string connectionName, TimeSpan connectionTimeout)
		{
			_uri = uri;
			_connectionName = connectionName;
			_connectionTimeout = connectionTimeout;
		}

		public async Task<ChannelContainer> AcquireChannel()
		{
			if (_connection != null)
				return GetOrCreateChannel();

			await _connectionLock.WaitAsync(_connectionTimeout);

			try
			{
				if (_connection?.IsOpen == false)
					ClearConnection();

				var connectionFactory = new ConnectionFactory
				{
					Uri = _uri,
					AutomaticRecoveryEnabled = true,
					DispatchConsumersAsync = true
				};

				_connection = connectionFactory.CreateConnection(_connectionName);
				return GetOrCreateChannel();
			}
			finally
			{
				_connectionLock.Release();
			}
		}

		private ChannelContainer GetOrCreateChannel()
		{
			while (!_availableChannelPool.IsEmpty)
			{
				if (_availableChannelPool.TryTake(out var poolChannel))
				{
					if (poolChannel.IsOpen)
					{
						lock (_lockUsedChannelCount)
							_usedChannelCount++;
						return new ChannelContainer(this, poolChannel);
					}
				}
			}

			if (_usedChannelCount > MaxChannelPool)
				throw new Exception("Too many channel allocated");

			var channel = _connection!.CreateModel();

			lock (_lockUsedChannelCount)
				_usedChannelCount++;

			return new ChannelContainer(this, channel);
		}

		public void ReleaseChannel(IModel channel)
		{
			if (channel.IsClosed)
				return;
			lock (_lockUsedChannelCount)
				_usedChannelCount--;
			_availableChannelPool.Add(channel);
		}

		private void ClearConnection()
		{
			_usedChannelCount = 0;
			_connection?.Dispose();
			_connection = null;
		}

		public void Dispose()
		{
			ClearConnection();
		}
	}
}
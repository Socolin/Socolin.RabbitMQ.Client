using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client;

[PublicAPI]
public interface IRabbitMqChannelManager : IDisposable
{
	Task<ChannelContainer> AcquireChannelAsync();
	void ReleaseChannel(IChannel channel);
}

public class RabbitMqChannelManager(
	Uri uri,
	string connectionName,
	TimeSpan connectionTimeout,
	ChannelType channelType,
	TimeSpan requestedHeartbeat
) : IRabbitMqChannelManager
{
	private const int MaxChannelPool = 90;

	private readonly SemaphoreSlim _connectionLock = new(1);
	private IConnection? _connection;

	private readonly ConcurrentBag<IChannel> _availableChannelPool = new();
	private int _usedChannelCount;

	public RabbitMqChannelManager(
		Uri uri,
		string connectionName,
		TimeSpan connectionTimeout,
		ChannelType channelType
	)
		: this(uri, connectionName, connectionTimeout, channelType, ConnectionFactory.DefaultHeartbeat)
	{
	}

	public async Task<ChannelContainer> AcquireChannelAsync()
	{
		if (_connection != null)
			return await GetOrCreateChannelAsync();

		await _connectionLock.WaitAsync(connectionTimeout);

		try
		{
			if (_connection?.IsOpen == false)
				ClearConnection();
			else if (_connection?.IsOpen == true)
				return await GetOrCreateChannelAsync();

			var connectionFactory = new ConnectionFactory
			{
				Uri = uri,
				AutomaticRecoveryEnabled = true,
				RequestedHeartbeat = requestedHeartbeat,
			};

			_connection = await connectionFactory.CreateConnectionAsync(connectionName + ":" + channelType);
			return await GetOrCreateChannelAsync();
		}
		finally
		{
			_connectionLock.Release();
		}
	}

	private async Task<ChannelContainer> GetOrCreateChannelAsync()
	{
		while (!_availableChannelPool.IsEmpty)
		{
			if (_availableChannelPool.TryTake(out var poolChannel))
			{
				if (poolChannel.IsOpen)
				{
					Interlocked.Increment(ref _usedChannelCount);
					return new ChannelContainer(this, poolChannel);
				}
			}
		}

		if (_usedChannelCount > MaxChannelPool)
			throw new Exception("Too many channel allocated");

		var channel = await _connection!.CreateChannelAsync();

		Interlocked.Increment(ref _usedChannelCount);

		return new ChannelContainer(this, channel);
	}

	public void ReleaseChannel(IChannel channel)
	{
		if (channel.IsClosed)
			return;
		Interlocked.Decrement(ref _usedChannelCount);
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
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Tests.Integration;

[SetUpFixture]
public class InitRabbitMqDocker
{
	private const string DockerInstanceName = "test_rabbitmq_library";
	protected static int RabbitMqPort { get; private set; }
	public static Uri RabbitMqUri { get; private set; }

	[OneTimeSetUp]
	public async Task Setup()
	{
		RabbitMqDockerHelper.StopDocker(DockerInstanceName);
		RabbitMqDockerHelper.RmDocker(DockerInstanceName);

		RabbitMqPort = GetFreePort();
		RabbitMqUri = new Uri($"amqp://localhost:{RabbitMqPort}");
		RabbitMqDockerHelper.CreateDocker(DockerInstanceName, RabbitMqPort);
		await WaitRabbitMqToBeReadyAsync();
	}

	[OneTimeTearDown]
	public void TearDown()
	{
		RabbitMqDockerHelper.StopDocker(DockerInstanceName);
		RabbitMqDockerHelper.RmDocker(DockerInstanceName);
	}

	public static void RestartRabbitMq()
	{
		RabbitMqDockerHelper.StopDocker(DockerInstanceName);
		RabbitMqDockerHelper.StartDocker(DockerInstanceName);
	}

	private static int GetFreePort()
	{
		var tcpListener = new TcpListener(IPAddress.Loopback, 0);
		tcpListener.Start();
		var port = ((IPEndPoint) tcpListener.LocalEndpoint).Port;
		tcpListener.Stop();
		return port;
	}

	private static async Task WaitRabbitMqToBeReadyAsync()
	{
		var sw = Stopwatch.StartNew();
		while (true)
		{
			if (sw.Elapsed > TimeSpan.FromSeconds(30))
				throw new Exception("Rabbitmq docker did not started in 30 seconds");
			try
			{
				await using var connection = await new ConnectionFactory {Uri = RabbitMqUri}.CreateConnectionAsync("WarmupIntegrationTest");
				break;
			}
			catch (Exception)
			{
				// Ignored
			}
		}
	}
}
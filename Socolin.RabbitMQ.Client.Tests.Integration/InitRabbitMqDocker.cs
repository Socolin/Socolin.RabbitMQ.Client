using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using RabbitMQ.Client;

namespace Socolin.RabbitMQ.Client.Tests.Integration
{
	[SetUpFixture]
	public class InitRabbitMqDocker
	{
		private const string DockerInstanceName = "test_rabbitmq_library";
		protected static int RabbitMqPort { get; private set; }
		public static Uri RabbitMqUri { get; private set; }

		[OneTimeSetUp]
		public void Setup()
		{
			RabbitMqDockerHelper.StopDocker(DockerInstanceName);
			RabbitMqDockerHelper.RmDocker(DockerInstanceName);

			RabbitMqPort = GetFreePort();
			RabbitMqUri = new Uri($"amqp://localhost:{RabbitMqPort}");
			RabbitMqDockerHelper.CreateDocker(DockerInstanceName, RabbitMqPort);
			WaitRabbitMqToBeReady();
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

		private static void WaitRabbitMqToBeReady()
		{
			var sw = Stopwatch.StartNew();
			while (true)
			{
				if (sw.Elapsed > TimeSpan.FromSeconds(30))
					throw new Exception("Rabbitmq docker did not started in 30 seconds");
				try
				{
					using var connection = new ConnectionFactory {Uri = RabbitMqUri}.CreateConnection("WarmupIntegrationTest");
					break;
				}
				catch (Exception)
				{
					// Ignored
				}
			}
		}
	}
}
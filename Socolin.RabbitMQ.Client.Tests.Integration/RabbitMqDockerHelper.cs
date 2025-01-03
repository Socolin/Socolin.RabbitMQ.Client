#nullable enable
using System;
using System.Diagnostics;

namespace Socolin.RabbitMQ.Client.Tests.Integration;

public class RabbitMqDockerHelper
{
	private const int RabbitMqPort = 5672;

	public static void StopDocker(string name)
	{
		Process? process = null;
		try
		{
			process = ExecuteCommand($"stop {name}", true);
		}
		finally
		{
			process?.Dispose();
		}
	}

	public static void RmDocker(string name)
	{
		Process? process = null;
		try
		{
			process = ExecuteCommand($"rm {name}", true);
		}
		finally
		{
			process?.Dispose();
		}
	}

	public static void StartDocker(string name)
	{
		Process? process = null;
		try
		{
			process = ExecuteCommand($"start {name}", true);
		}
		finally
		{
			process?.Dispose();
		}
	}

	public static void CreateDocker(string name, int port)
	{
		Process? process = null;
		try
		{
			process = ExecuteCommand($"run -d -p {port}:{RabbitMqPort} --name {name} rabbitmq:3");
		}
		catch (Exception)
		{
			StopDocker(name);
			RmDocker(name);
			throw;
		}
		finally
		{
			process?.Dispose();
		}
	}

	private static Process ExecuteCommand(string dockerCmd, bool ignoreExitCode = false)
	{
		var process = Process.Start("docker", dockerCmd);
		if (process == null)
			throw new Exception("Failed to execute docker command");
		process.WaitForExit();
		if (!ignoreExitCode && process.ExitCode != 0)
			throw new Exception($"Execution failed: `{dockerCmd}`");
		return process;
	}
}
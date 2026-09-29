using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Socolin.RabbitMQ.Client.Options.Client;
using Socolin.RabbitMQ.Client.Pipes.Client;

namespace Socolin.RabbitMQ.Client.Tests.Unit;

public class EnqueueMessageOptionsTests
{
    [Test]
    public void CanCreateWithoutPriority()
    {
        var options = new EnqueueMessageOptions();

        Assert.That(options.Priority, Is.Null);
    }

    [TestCase((byte)0)]
    [TestCase((byte)31)]
    [TestCase(null)]
    public void CanCreate_WithValidPriority(byte? priority)
    {
        var options = new EnqueueMessageOptions { Priority = priority };

        Assert.That(options.Priority, Is.EqualTo(priority));
    }

    [Test]
    public void CanCreate_WithPriorityAboveNine()
    {
        var options = new EnqueueMessageOptions { Priority = 10 };

        Assert.That(options.Priority, Is.EqualTo(10));
    }

    [Test]
    public async Task EnqueueRejectsPriorityAboveThirtyOne()
    {
        var client = new RabbitMqEnqueueQueueClient(ReadOnlyMemory<IClientPipe>.Empty);
        var options = new EnqueueMessageOptions { Priority = 32 };

        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await client.EnqueueMessageAsync("message", options));

        Assert.That(exception!.ParamName, Is.EqualTo("priority"));
    }
}

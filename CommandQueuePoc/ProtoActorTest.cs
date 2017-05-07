using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Proto;
using Xunit;

namespace CommandQueuePoc
{
    public class ProtoActorTest
    {
        [Fact]
        public async Task InOrderCommandHandlerTest()
        {
            MyCommandHandler.Commands = new List<string>();

            var handler = new ProtoActorHandler();

            handler.Send(new MyCommand("command1", 300));
            handler.Send(new MyCommand("command2", 250));
            handler.Send(new MyCommand("command3", 1));
            handler.Send(new MyCommand("command4", 20));

            Thread.Sleep(1000);
            var commands = MyCommandHandler.Commands;
            commands.Should().ContainInOrder("command1", "command2", "command3", "command4");
        }
    }

    class ProtoActorHandler
    {
        PID pid;
        public ProtoActorHandler()
        {
            var props = Actor.FromProducer(() => new ProtoHandler());
            pid = Actor.Spawn(props);
        }

        public void Send<T>(T command)
        {
            pid.Tell(command);
        }
    }

    class ProtoHandler : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;
            if (msg is MyCommand r)
            {
                new MyCommandHandler().Handle(r);
            }
            return Actor.Done;
        }
    }
}

using System;
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

            var t1 = handler.SendAsync(new MyCommand("command1", 300));
            var t2 = handler.SendAsync(new MyCommand("command2", 250));
            var t3 = handler.SendAsync(new MyCommand("command3", 1));
            var t4 = handler.SendAsync(new MyCommand("command4", 20));

            await Task.WhenAll(t1, t2, t3, t4);
            var commands = MyCommandHandler.Commands;
            commands.Should().ContainInOrder("command1", "command2", "command3", "command4");
        }

        [Fact]
        public void ThrowExceptionTest()
        {
            MyCommandHandler.Commands = new List<string>();

            var handler = new ProtoActorHandler();

            Func<Task> act = () => handler.SendAsync(new MyCommand("command5", 300));
            act.ShouldThrow<InvalidOperationException>();
        }
    }

    class ProtoActorHandler
    {
        PID pid;
        public ProtoActorHandler()
        {
            var props = Actor.FromProducer(() => new CommandHandlerProtoHandler());
            pid = Actor.Spawn(props);
        }

        public async Task SendAsync<T>(T command)
        {
            var result = await pid.RequestAsync<object>(command).ConfigureAwait(false);
            if (result is Exception ex)
            {
                throw new InvalidOperationException("error in command execution", ex);
            }
        }
    }

    class CommandHandlerProtoHandler : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;
            if (msg is MyCommand r)
            {
                try
                {
                    new MyCommandHandler().Handle(r);
                    context.Sender?.Tell("Ok");
                }
                catch (Exception ex)
                {
                    context.Sender?.Tell(ex);
                }
            }
            return Actor.Done;
        }
    }
}

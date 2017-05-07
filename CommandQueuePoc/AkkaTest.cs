using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using FluentAssertions;
using Xunit;

namespace CommandQueuePoc
{
    public class AkkaTest
    {
        [Fact]
        public async Task InOrderCommandHandlerTest()
        {
            MyCommandHandler.Commands = new List<string>();

            var handler = new AkkaHandler();

            var t1 = handler.SendAsync(new MyCommand("command1", 300));
            var t2 = handler.SendAsync(new MyCommand("command2", 250));
            var t3 = handler.SendAsync(new MyCommand("command3", 1));
            var t4 = handler.SendAsync(new MyCommand("command4", 20));
            await Task.WhenAll(t1, t2, t3, t4);

            var commands = MyCommandHandler.Commands;
            commands.Should().ContainInOrder("command1", "command2", "command3", "command4");
        }
    }

    class AkkaHandler
    {
        IActorRef MyActor { get; }
        ActorSystem ActorSystem { get; }

        public AkkaHandler()
        {
            ActorSystem = ActorSystem.Create("app");
            //here you would register your toplevel actors
            MyActor = ActorSystem.ActorOf<AkkaActor>();
        }

        public Task SendAsync<T>(T command)
        {
            return MyActor.Ask<object>(command);
        }
    }

    class AkkaActor : ReceiveActor
    {
        public AkkaActor()
        {
            ReceiveAny(ReceiveMessage);
        }

        private void ReceiveMessage(object message)
        {
            if (message is MyCommand r)
            {
                new MyCommandHandler().Handle(r);
                Sender.Tell("ok");
            }
        }
    }
}

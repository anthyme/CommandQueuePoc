using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using paramore.brighter.commandprocessor;
using paramore.brighter.commandprocessor.Logging;
using Xunit;

namespace CommandQueuePoc
{
    public class BrighterTest
    {
        [Fact]
        public async Task InOrderCommandHandlerTest()
        {
            MyCommandHandler.Commands = new List<string>();

            var handler = new BrighterHandler();

            var t1 = handler.SendAsync(new MyCommand("command1", 300));
            var t2 = handler.SendAsync(new MyCommand("command2", 250));
            var t3 = handler.SendAsync(new MyCommand("command3", 1));
            var t4 = handler.SendAsync(new MyCommand("command4", 20));

            await Task.WhenAll(t1, t2, t3, t4);

            var commands = MyCommandHandler.Commands;
            commands.Should().ContainInOrder("command1", "command2", "command3", "command4");
        }
    }

    class BrighterHandler
    {
        readonly CommandProcessor commandProcessor;

        public BrighterHandler()
        {
            var logger = LogProvider.For<BrighterHandler>();

            var registry = new SubscriberRegistry();
            registry.Register<MyCommand, GreetingCommandHandler>();
            registry.RegisterAsync<MyCommand, GreetingCommandHandlerAsync>();

            var builder = CommandProcessorBuilder.With()
                .Handlers(new HandlerConfiguration(registry, new SimpleHandlerFactoryAsync()))
                .DefaultPolicy()
                .NoTaskQueues()
                //.TaskQueues(new MessagingConfiguration(
                //    new InMemoryMessageStore(), 
                //    (IAmAMessageProducerAsync)new MessageProducer(), 
                //    new MessageMapperRegistry(new MessageMapperFactory())))
                .RequestContextFactory(new InMemoryRequestContextFactory())
                ;

            commandProcessor = builder.Build();
        }

        public Task SendAsync<T>(T command) where T : class, IRequest
        {
            return commandProcessor.SendAsync(command);
        }
    }

    internal class SimpleHandlerFactoryAsync : IAmAHandlerFactoryAsync
    {
        public IHandleRequestsAsync Create(Type handlerType)
        {
            return new GreetingCommandHandlerAsync(new MyCommandHandler());
        }

        public void Release(IHandleRequestsAsync handler)
        {
        }
    }

    internal class SimpleHandlerFactory : IAmAHandlerFactory
    {
        public IHandleRequests Create(Type handlerType)
        {
            return new GreetingCommandHandler(new MyCommandHandler());
        }

        public void Release(IHandleRequests handler)
        {
        }
    }

    public class MessageMapperFactory : IAmAMessageMapperFactory
    {
        public IAmAMessageMapper Create(Type messageMapperType)
        {
            return new TaskReminderCommandMessageMapper();
        }
    }

    public class TaskReminderCommandMessageMapper : IAmAMessageMapper<MyCommand>
    {
        public Message MapToMessage(MyCommand request)
        {
            var header = new MessageHeader(messageId: request.Id, topic: "Task.Reminder", messageType: MessageType.MT_COMMAND);
            var body = new MessageBody(JsonConvert.SerializeObject(request));
            var message = new Message(header, body);
            return message;
        }

        public MyCommand MapToRequest(Message message)
        {
            return JsonConvert.DeserializeObject<MyCommand>(message.Body.Value);
        }
    }

    public class MessageProducer : IAmAMessageProducer, IAmAMessageProducerAsync
    {
        public async Task SendAsync(Message message) { }
        public void Send(Message message) { }
        public void Dispose() { }
    }

    class GreetingCommandHandler : RequestHandler<MyCommand>
    {
        private readonly MyCommandHandler handler;

        public GreetingCommandHandler(MyCommandHandler handler)
        {
            this.handler = handler;
        }

        public override MyCommand Handle(MyCommand command)
        {
            return base.Handle(handler.Handle(command));
        }
    }


    class GreetingCommandHandlerAsync : RequestHandlerAsync<MyCommand>
    {
        private readonly MyCommandHandler handler;

        public GreetingCommandHandlerAsync(MyCommandHandler handler)
        {
            this.handler = handler;
        }

        public override Task<MyCommand> HandleAsync(MyCommand command, CancellationToken? ct = null)
        {
            return base.HandleAsync(handler.Handle(command), ct);
        }
    }
}

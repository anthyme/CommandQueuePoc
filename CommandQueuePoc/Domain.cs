using System;
using System.Collections.Generic;
using System.Threading;
using paramore.brighter.commandprocessor;

namespace CommandQueuePoc
{
    class MyCommandHandler
    {
        public static List<string> Commands = new List<string>();

        public MyCommand Handle(MyCommand command)
        {
            if(command.Name == "command5") throw new ArgumentException("command5");
            Thread.Sleep(command.Sleep);
            Commands.Add(command.Name);
            return command;
        }
    }

    public class MyCommand : IRequest
    {
        public MyCommand()
        {
            Id = Guid.NewGuid();
        }

        public MyCommand(string name, int sleep) : this()
        {
            Name = name;
            Sleep = sleep;
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Sleep { get; set; }
    }
}

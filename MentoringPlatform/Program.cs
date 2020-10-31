using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MentoringPlatform;


namespace MentoringPlatform
{


    public class Program
    {

        public static void Main(string[] args)
        {
            DomainEvents.RegisterHandler(() => new RegisterMentorHandler());
            DomainEvents.Register((RegisterMentor x) => Console.WriteLine("Action Callback"));


            DomainEvents.Raise(new RegisterMentor("first", "17-04-1992"));


            do
            {
                Console.WriteLine("Enter a command:");
                var command = Console.ReadLine();

                if (command == "exit")
                {
                    break;
                }

                if (command == null)
                {
                    continue;
                }

                var mentorsRegister = "mentors register";
                if (command.StartsWith(mentorsRegister))
                {
                    var arguments = command.Substring(mentorsRegister.Length + 1).Split(" ");
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine("Arguments invalid for register [name] [dateOfBirth]");
                        continue;
                    }
                    var name = arguments[0];
                    var dateOfBirth = arguments[1];

                    DomainEvents.Raise(new RegisterMentor( name, dateOfBirth));
                }

                if (command.StartsWith("mentors show"))
                {

                }

                var classCreate = "class create";
                if (command.StartsWith(classCreate))
                {
                    var arguments = command.Substring(classCreate.Length + 1).Split(" ");

                    if (arguments.Length != 2)
                    {
                        Console.WriteLine($"Arguments invalid for {classCreate} [name] [totalClassSize]");
                        continue;
                    }

                    var className = arguments[0];
                    var totalClassSizeInput = arguments[1];

                    int totalClassSize;
                    int.TryParse(totalClassSizeInput, out totalClassSize);
                    DomainEvents.Raise(new ClassCreated(className, totalClassSize));
                }

                var classCancel = "class cancel";
                if (command.StartsWith(classCancel))
                {
                    var arguments = command.Substring(classCreate.Length + 1).Split(" ");

                    if (arguments.Length != 1)
                    {
                        Console.WriteLine($"Arguments invalid for {classCancel} [name]");
                        continue;
                    }

                    var className = arguments[0];

                    DomainEvents.Raise(new ClassCancelled(className));

                }

                var classShow = "class show";
                if (command.StartsWith(classShow))
                {
                    var arguments = command.Substring(classShow.Length + 1).Split(" ");

                    if (arguments.Length != 1)
                    {
                        Console.WriteLine($"Arguments invalid for {classShow} [name]");
                        continue;
                    }

                    Console.WriteLine("Show classes");
                }


            } while (true);

        }

    }

    public class Class: AggregateRoot<string>
    {
        private int totalClassSize;
        public override string Id { get; }

        public Class(string name, IEnumerable<object> events)
        {
            Id = name;
            foreach (dynamic @event in events)
            {
                Apply(@event);
            }
        }

        public Class(string name, int totalClassSize)
        {
            Id = name;

            var @event = new ClassCreated(name, totalClassSize);
            Apply(@event);
            Append(@event);

        }

        private void Apply(ClassCreated @event)
        {
            totalClassSize = @event.totalClassSize;
        }

        public void Cancel(string name)
        {
            if (name == null)
            {
                throw new InvalidOperationException($"The class with name: {name} has not been created");
            }

            var @event = new ClassCancelled(name);
            Apply(@event);
            Append(@event);
        }

        private void Apply(ClassCancelled @event)
        {
            isCancelled = true;
        }
        public bool isCancelled { get; set; }
    }

    public class ClassCancelled
    {
        public string className { get; }

        public ClassCancelled(string className)
        {
            this.className = className;
        }

    }

    public class ClassCreated
    {
        public string className { get; }
        public int totalClassSize { get; }

        public ClassCreated(string className, int totalClassSize)
        {
            this.className = className;
            this.totalClassSize = totalClassSize;
        }
    }


    public class RegisterMentorHandler: DomainEvents.Handles<RegisterMentor>
    {
        public void Handle(RegisterMentor @event)
        {
            Console.WriteLine($"mentor registered with name {@event.name} and date of birth: {@event.dateOfBirth}");

        }
    }

    public class RegisterMentor
    {
        public string name { get; }
        public string dateOfBirth { get; }

        public RegisterMentor(string name, string dateOfBirth)
        {
            this.name = name;
            this.dateOfBirth = dateOfBirth;
        }
    }
}

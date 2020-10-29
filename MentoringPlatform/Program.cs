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


            DomainEvents.Raise(new RegisterMentor());


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

                if (command.StartsWith("register"))
                {
                    var arguments = command.Substring(9).Split(" ");
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine("Arguments invalid for register [name] [dateOfBirth]");
                        continue;
                    }
                    var name = arguments[0];
                    var dateOfBirth = arguments[1];

                    DomainEvents.Raise(new RegisterMentor{ name = name, dateOfBirth = dateOfBirth});
                }


            } while (true);

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
        public string name;
        public string dateOfBirth;
    }
}

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
        }
    }

    public class RegisterMentorHandler: DomainEvents.Handles<RegisterMentor>
    {
        public void Handle(RegisterMentor @event)
        {
            Console.WriteLine($"handle register mentor event");

        }
    }

    public class RegisterMentor
    {

    }
}

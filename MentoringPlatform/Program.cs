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

    public interface IClassRepository
    {
        void Add(Class @class);
        Class this[string className] { get; }
    }

    internal class ClassRepository: Repository<string, Class>, IClassRepository
    {
        protected override Class CreateInstance(string id, IEnumerable<object> events)
        {
            return new Class(id, events);
        }
    }

    public class ClassState
    {
        public string name { get; set; }
        public int totalClassSize { get; set; }
        public bool isCancelled { get; set; }
    }

    public interface IClassStateQuery
    {
        IEnumerable<ClassState> GetClassStates();
        ClassState GetClassState(string className);

        IEnumerable<ClassState> GetCancelledClasses();

        void AddClassState(string className, int totalClassSize, bool isCancelled);
        void SetCancelled(string className, bool isCancelled);
    }

    class ClassStateQuery: IClassStateQuery
    {
        private readonly Dictionary<string, ClassState> states = new Dictionary<string, ClassState>();

        public IEnumerable<ClassState> GetClassStates()
        {
            return states.Values;
        }

        public ClassState GetClassState(string className)
        {
            return states[className];
        }

        public IEnumerable<ClassState> GetCancelledClasses()
        {
            return states.Values.Where(@class => @class.isCancelled);
        }

        public void AddClassState(string className, int totalClassSize, bool isCancelled)
        {
            var state = new ClassState { name = className,totalClassSize = totalClassSize, isCancelled = isCancelled};
            states.Add(className, state);
        }

        public void SetCancelled(string className, bool isCancelled)
        {
            states[className].isCancelled = isCancelled;
        }
    }

    class ClassStateHandler : DomainEvents.Handles<ClassCreated>, DomainEvents.Handles<ClassCancelled>
    {
        private readonly IClassStateQuery classStateQuery;

        public ClassStateHandler(IClassStateQuery classStateQuery)
        {
            this.classStateQuery = classStateQuery;
        }

        public void Handle(ClassCreated @event)
        {
            Console.WriteLine($"Class was created with name: {@event.className} and size: {@event.totalClassSize}");
            classStateQuery.AddClassState(@event.className, @event.totalClassSize, false);
        }

        public void Handle(ClassCancelled @event)
        {
            Console.WriteLine($"Class: {@event.className} was cancelled");
            classStateQuery.SetCancelled(@event.className, true);
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

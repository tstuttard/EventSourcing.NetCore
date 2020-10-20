using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace MentoringPlatform
{
    public interface IEventStorage: IDisposable
    {
        IAggregateRootStorage<Tid> GetAggregateRootStorage<TAggregateRoot, Tid>()
            where TAggregateRoot : AggregateRoot<Tid>;
    }

    public interface IAggregateRootStorage<in Tid>
    {
        void Append(Tid id, IEnumerable<object> events);
        IEnumerable<object> this[Tid id] { get; }
    }

    public class EventStorage: IEventStorage
    {
        private readonly Dictionary<Type, dynamic> stores = new Dictionary<Type, dynamic>();

        public IAggregateRootStorage<Tid> GetAggregateRootStorage<TAggregateRoot, Tid>()
            where TAggregateRoot : AggregateRoot<Tid>
        {
            dynamic store;
            if (!stores.TryGetValue(typeof(TAggregateRoot), out store))
            {
                store = new AggregateRootStorage<Tid>();
                stores.Add(typeof(TAggregateRoot), store);
            }

            return store;
        }

        public void Dispose()
        {
            stores.Clear();
        }
    }

    class AggregateRootStorage<Tid>: IAggregateRootStorage<Tid>
    {
        private readonly Dictionary<Tid, List<object>> store = new Dictionary<Tid, List<object>>();

        public void Append(Tid id, IEnumerable<object> events)
        {
            List<object> aggregateRootEvents;

            if (!store.TryGetValue(id, out aggregateRootEvents))
            {
                aggregateRootEvents = new List<object>();
                store.Add(id, aggregateRootEvents);
            }

            aggregateRootEvents.AddRange(events);
        }

        public IEnumerable<object> this[Tid id]
        {
            get { return store[id]; }
        }
    }

    public interface IUncommittedEvents: IEnumerable<object>
    {
        bool HasEvents { get; }
        void Commit();
    }

    internal class UncommittedEvents: IUncommittedEvents
    {
        private readonly List<object> events = new List<object>();

        public void Append(object @event)
        {
            events.Add(@event);
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return events.GetEnumerator();
        }

        public bool HasEvents
        {
            get { return events.Count != 0; }
        }

        void IUncommittedEvents.Commit()
        {
            events.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return events.GetEnumerator();
        }
    }

    public interface IAggregateRoot<out Tid>
    {
        Tid Id { get; }

        IUncommittedEvents UncommittedEvents { get; }
    }

    public abstract class AggregateRoot<Tid>: IAggregateRoot<Tid>
    {
        private readonly UncommittedEvents uncommittedEvents = new UncommittedEvents();

        protected void Replay(IEnumerable<object> events)
        {
            dynamic me = this;
            foreach (var @event in events)
                me.Apply(@event);
        }

        protected void Append(object @event)
        {
            uncommittedEvents.Append(@event);
        }

        public abstract Tid Id { get; }

        IUncommittedEvents IAggregateRoot<Tid>.UncommittedEvents
        {
            get { return uncommittedEvents; }
        }
    }

    internal interface ISessionItem
    {
        void SubmitChanges();
    }

    public abstract class Repository<Tid, TAggregateRoot>: ISessionItem
        where TAggregateRoot : AggregateRoot<Tid>
    {
        private readonly Dictionary<Tid, TAggregateRoot> users = new Dictionary<Tid, TAggregateRoot>();

        private readonly IAggregateRootStorage<Tid> aggregateRootStorage;

        protected Repository()
        {
            aggregateRootStorage = Session.Enlist(this);
        }

        public void Add(TAggregateRoot user)
        {
            users.Add(user.Id, user);
        }

        public TAggregateRoot this[Tid id]
        {
            get { return Find(id) ?? Load(id); }
        }

        private TAggregateRoot Find(Tid id)
        {
            TAggregateRoot user;
            return users.TryGetValue(id, out user) ? user : null;
        }

        private TAggregateRoot Load(Tid id)
        {
            var events = aggregateRootStorage[id];
            var user = CreateInstance(id, events);

            users.Add(id, user);
            return user;
        }

        protected abstract TAggregateRoot CreateInstance(Tid id, IEnumerable<object> events);

        public void SubmitChanges()
        {
            foreach (IAggregateRoot<Tid> user in users.Values)
            {
                var uncommittedEvents = user.UncommittedEvents;

                if (uncommittedEvents.HasEvents)
                {
                    aggregateRootStorage.Append(user.Id, uncommittedEvents);

                    PublishEvents(uncommittedEvents);
                    uncommittedEvents.Commit();
                }
            }
        }

        protected void PublishEvents(IUncommittedEvents uncommittedEvents)
        {
            foreach (dynamic @event in uncommittedEvents)
            {
                DomainEvents.Raise(@event);
            }
        }
    }

    public interface ISessionFactory: IDisposable
    {
        ISession OpenSession();
    }

    public class SessionFactory: ISessionFactory
    {
        private readonly IEventStorage eventStorage;

        public SessionFactory(IEventStorage eventStorage)
        {
            this.eventStorage = eventStorage;
        }

        public void Dispose()
        {
            eventStorage.Dispose();
        }

        public ISession OpenSession()
        {
            return new Session(eventStorage);
        }
    }

    public interface ISession: IDisposable
    {
        void SubmitChanges();
    }

    public class Session: ISession
    {
        private readonly IEventStorage eventStorage;
        private readonly HashSet<ISessionItem> enlistedItems = new HashSet<ISessionItem>();

        [ThreadStatic] private static Session current;

        internal Session(IEventStorage eventStorage)
        {
            this.eventStorage = eventStorage;

            if (current != null)
            {
                throw new InvalidOperationException("Cannot nest unit of work");
            }

            current = this;
        }

        private static Session Current
        {
            get { return current; }
        }

        public void SubmitChanges()
        {
            foreach (var enlisted in enlistedItems)
            {
                enlisted.SubmitChanges();
            }

            enlistedItems.Clear();
        }

        public void Dispose()
        {
            current = null;
        }

        internal static IAggregateRootStorage<Tid> Enlist<Tid, TAggregateRoot>(
            Repository<Tid, TAggregateRoot> repository)
            where TAggregateRoot : AggregateRoot<Tid>
        {
            var unitOfWork = Current;
            unitOfWork.enlistedItems.Add(repository);
            return unitOfWork.eventStorage.GetAggregateRootStorage<TAggregateRoot, Tid>();
        }
    }

    public static class DomainEvents
    {
        [ThreadStatic] private static List<Delegate> actions;

        private static List<Handler> handlers;

        public static void Register<T>(Action<T> callback)
        {
            if (actions == null) actions = new List<Delegate>();

            actions.Add(callback);
        }

        public static void Raise<T>(T @event)
        {
            if (actions != null)
            {
                foreach (var action in actions.OfType<Action<T>>())
                {
                    // (action)(@event);
                }
            }

            if (handlers == null) return;
            foreach (var handler in from h in handlers where h.Handles<T>() select h.CreateInstance<T>())
            {
                handler.Handle(@event);
            }
        }

        private abstract class Handler
        {
            public abstract bool Handles<E>();
            public abstract Handles<E> CreateInstance<E>();
        }

        private class Handler<T>: Handler
        {
            private readonly Func<T> factory;

            public Handler(Func<T> factory)
            {
                this.factory = factory;
            }

            public override bool Handles<E>()
            {
                return typeof(Handles<E>).IsAssignableFrom(typeof(T));
            }

            public override Handles<E> CreateInstance<E>()
            {
                return (Handles<E>)factory();
            }
        }

        public interface Handles<in T>
        {
            void Handle(T @event);
        }

        public static void RegisterHandler(Func<RegisterMentorHandler> func)
        {

        }
    }
}

using System;

namespace EventStoreBasics
{
    public class Repository<T>: IRepository<T> where T : IAggregate
    {
        private readonly IEventStore eventStore;

        public Repository(IEventStore eventStore)
        {
            this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        }

        public T Find(Guid id)
        {
            var aggregate = eventStore.AggregateStream<T>(id);

            return aggregate;
        }

        public void Add(T aggregate)
        {
            eventStore.Store(aggregate);

        }

        public void Update(T aggregate)
        {
            eventStore.Store(aggregate);
        }

        public void Delete(T aggregate)
        {
            eventStore.Store(aggregate);
        }
    }
}

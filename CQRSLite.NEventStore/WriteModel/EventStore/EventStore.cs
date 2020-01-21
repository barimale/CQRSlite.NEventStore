using CQRSlite.Events;
using NEventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CQRSLite.NEventStore.WriteModel.EventStore
{
    public class EventStore : IEventStore
    {
        private readonly IEventPublisher Publisher;
        private readonly IStoreEvents Store;

        public EventStore(IEventPublisher publisher, IStoreEvents store)
        {
            Publisher = publisher;
            Store = store;
        }

        public async Task Save(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var @event in events)
            {
                using (var stream = Store.OpenStream(@event.Id, 0, int.MaxValue))
                {
                    stream.Add(new EventMessage { Body = @event });
                    stream.CommitChanges(Guid.NewGuid());
                }

                await Publisher.Publish(@event, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task<IEnumerable<IEvent>> Get(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default)
        {
            using (var stream = Store.OpenStream(aggregateId, 0))
            {
                var myEvents = stream.CommittedEvents;
                if (myEvents == null || myEvents.Count == 0)
                {
                    return Task.FromResult(new List<IEvent>().AsEnumerable());
                }

                var allOfThem = new List<IEvent>();
                foreach (var ev in myEvents)
                {
                    var body = ev.Body as IEvent;
                    if (body.Version > fromVersion)
                        allOfThem.Add(body);
                }

                return Task.FromResult(allOfThem.AsEnumerable());
            }
        }
    }
}
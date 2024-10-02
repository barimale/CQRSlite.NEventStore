using CQRSlite.Extensions.Common;
using CQRSlite.Extensions.WriteModel.Domain;
using CQRSlite.Snapshotting;
using NEventStore;
using NEventStore.Persistence;
using NEventStore.Persistence.Sql.SqlDialects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Snapshot = CQRSlite.Snapshotting.Snapshot;
using IConnectionFactory = NEventStore.Persistence.Sql;
using NEventStoreSnapshot = NEventStore.Snapshot;

namespace CQRSLite.NEventStore.WriteModel.EventStore.SnapshotStore
{
    public class SnapshotStore : ISnapshotStore, IStoreEvents, ISnapshotManager
    {
        private readonly ConcurrentDictionary<Guid, Snapshot> snapshotContentDictionary = new ConcurrentDictionary<Guid, Snapshot>();

        private IStoreEvents eventStore;
        private readonly IConnectionFactory.IConnectionFactory _enviromentConnectionFactory;

        public SnapshotStore(IConnectionFactory.IConnectionFactory enviromentConnectionFactory)
        {
            _enviromentConnectionFactory = enviromentConnectionFactory;
        }

        private IStoreEvents EventStore
        {
            get
            {
                if (eventStore == null)
                {
                    eventStore = Wireup
                    .Init()
                    .UsingSqlPersistence(_enviromentConnectionFactory)
                    .WithDialect(new MsSqlDialect())
                    .InitializeStorageEngine()
                    .UsingBinarySerialization()
                    .Build();
                }

                return eventStore;
            }
        }

        public IPersistStreams Advanced => EventStore.Advanced;

        public bool Save(string streamId, int streamVersion)
        {
            try
            {
                var innerPayload = snapshotContentDictionary
                    .Values
                    .Select(p => new TrackedTag(p.Id, p.Version))
                    .ToList<object>();

                var snap = new NEventStoreSnapshot(
                    streamId,
                    streamVersion,
                    innerPayload);

                var result = EventStore.Advanced.AddSnapshot(snap);

                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public void Load(string streamId)
        {
            try
            {
                var snap = EventStore
                    .Advanced
                    .GetSnapshot(streamId, int.MaxValue);

                if (snap == null || snap.Payload == null)
                {
                    return;
                }

                var items = snap.Payload as List<object>;
                switch (items)
                {
                    case null:
                        return;

                    default:
                        items.AsParallel().ForAll(p =>
                        {
                            var item = p as TrackedTag;
                            var result = new BaseSnap
                            {
                                Id = item.Id,
                                Version = item.Version
                            };

                            snapshotContentDictionary.TryAdd(result.Id, result);
                        });

                        return;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public IEventStream CreateStream(string bucketId, string streamId)
        {
            return EventStore.CreateStream(bucketId, streamId);
        }

        public void Dispose()
        {
            if (eventStore != null)
                EventStore.Dispose();
        }

        public Task<Snapshot> Get(Guid id, CancellationToken cancellationToken = default)
        {
            snapshotContentDictionary.TryGetValue(id, out Snapshot result);

            return Task.FromResult(result);
        }

        public async Task Save(Snapshot snapshot, CancellationToken cancellationToken = default)
        {
            try
            {
                snapshotContentDictionary.AddOrUpdate(snapshot.Id, snapshot, (key, oldValue) => snapshot);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public IEventStream OpenStream(string bucketId, string streamId, int minRevision, int maxRevision)
        {
            return EventStore.OpenStream(bucketId, streamId, minRevision, maxRevision);
        }

        public IEventStream OpenStream(ISnapshot snapshot, int maxRevision)
        {
            return EventStore.OpenStream(snapshot, maxRevision);
        }
    }
}
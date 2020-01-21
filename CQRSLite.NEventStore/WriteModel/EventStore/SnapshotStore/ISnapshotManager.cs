namespace CQRSLite.NEventStore.WriteModel.EventStore.SnapshotStore
{
    public interface ISnapshotManager
    {
        bool Save(string streamId, int streamVersion);

        void Load(string streamId);
    }
}
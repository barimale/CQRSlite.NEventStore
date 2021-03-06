﻿using CQRSlite.Domain;
using CQRSlite.Events;
using CQRSlite.Snapshotting;
using CQRSLite.NEventStore.WriteModel.EventStore;
using CQRSLite.NEventStore.WriteModel.EventStore.SnapshotStore;
using NEventStore;
using Ninject.Modules;
using IConnectionFactory = NEventStore.Persistence.Sql.IConnectionFactory;

namespace CQRSLite.NEventStore.Configuration
{
    public class Bindings : NinjectModule
    {
        public override void Load()
        {
            try
            {
                Bind<IConnectionFactory>()
                .To<EnviromentConnectionFactory>()
                .InSingletonScope()
                .WithConstructorArgument("connectionStringName", "EventStore");

                Bind<IStoreEvents, ISnapshotStore, ISnapshotManager>()
               .To<SnapshotStore>()
               .InSingletonScope();

                Bind<IEventStore>()
                    .To<EventStore>()
                    .InSingletonScope();

                Bind<IRepository>()
                    .To<Repository>()
                    .WhenInjectedExactlyInto(typeof(SnapshotRepository))
                    .InSingletonScope();

                Bind<ISnapshotStrategy>()
                    .To<CustomSnapshotStrategy>()
                    .InSingletonScope();

                Bind<IRepository>()
                    .To<SnapshotRepository>()
                    .WithConstructorArgument(typeof(ISnapshotStore))
                    .WithConstructorArgument(typeof(ISnapshotStrategy))
                    .WithConstructorArgument(typeof(IRepository))
                    .WithConstructorArgument(typeof(IEventStore));
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
        }
    }
}

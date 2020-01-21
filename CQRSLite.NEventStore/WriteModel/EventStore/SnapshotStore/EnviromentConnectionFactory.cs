using NEventStore.Persistence.Sql;
using System;
using System.Data;
using System.Data.Common;
using Persistance = NEventStore.Persistence;

namespace CQRSLite.NEventStore.WriteModel.EventStore.SnapshotStore
{
    public class EnviromentConnectionFactory : IConnectionFactory
    {
        private readonly string databaseConnectionString;
        private readonly DbProviderFactory _dbProviderFactory;

        public EnviromentConnectionFactory(string connectionString)
        {
            databaseConnectionString = connectionString;
            _dbProviderFactory = System.Data.SqlClient.SqlClientFactory.Instance;
        }

        public IDbConnection Open()
        {
            return new ConnectionScope(databaseConnectionString, OpenInternal);
        }

        public Type GetDbProviderFactoryType()
        {
            return _dbProviderFactory.GetType();
        }

        public DbProviderFactory GetDbProviderFactory()
        {
            return _dbProviderFactory;
        }

        private IDbConnection OpenInternal()
        {
            try
            {
                DbConnection connection = _dbProviderFactory.CreateConnection();
                connection.ConnectionString = databaseConnectionString;

                connection.Open();

                return connection;
            }
            catch (Exception e)
            {
                throw new Persistance.StorageUnavailableException(e.Message, e);
            }
        }
    }
}
using System;
using Dapper;
using Npgsql;

namespace EventStoreBasics
{
    public class EventStore: IDisposable, IEventStore
    {
        private readonly NpgsqlConnection databaseConnection;

        public EventStore(NpgsqlConnection databaseConnection)
        {
            this.databaseConnection = databaseConnection;
        }

        public void Init()
        {
            // See more in Greg Young's "Building an Event Storage" article https://cqrs.wordpress.com/documents/building-event-storage/
            CreateStreamsTable();
        }

        private void CreateStreamsTable()
        {
            const string CreateStreamTablesSql = @"
CREATE TABLE IF NOT EXISTS streams(
id UUID NOT NULL PRIMARY KEY ,
type TEXT NOT NULL,
version BIGINT NOT NULL
);";
            databaseConnection.Execute(CreateStreamTablesSql);

        }

        public void Dispose()
        {
            databaseConnection.Dispose();
        }
    }
}

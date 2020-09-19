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
            CreateEventsTable();
        }

        private void CreateStreamsTable()
        {
            const string CreateStreamsTableSQL =
                @"CREATE TABLE IF NOT EXISTS streams(
                      id             UUID                      NOT NULL    PRIMARY KEY,
                      type           TEXT                      NOT NULL,
                      version        BIGINT                    NOT NULL
                  );";
            databaseConnection.Execute(CreateStreamsTableSQL);
        }

        private void CreateEventsTable()
        {
            const string CreateEventsTableSql =
                @"create table if not exists events(
id UUID not null primary key,
data jsonb not null,
stream_id UUID not null,
type TEXT not null,
version BIGINT not null,
created timestamp with time zone not null

);";

            databaseConnection.Execute(CreateEventsTableSql);

        }

        public void Dispose()
        {
            databaseConnection.Dispose();
        }
    }
}

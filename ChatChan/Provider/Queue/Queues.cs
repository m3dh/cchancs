namespace ChatChan.Provider.Queue
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Provider;
    using ChatChan.Provider.Executor;

    using Microsoft.Extensions.Logging;

    public interface IQueueEvent
    {
        int DataType { get; }
        string DataJson { get; }
        bool Processed { get; }
    }

    public interface IMessageQueue
    {
        Task<IQueueEvent> Pop();
        Task Dequeue(IQueueEvent queueEvent);
        Task PushOne(int eventType, string eventData);
    }

    public class CoreDbTableQueue : IMessageQueue
    {
        private readonly string queueTable;
        private readonly MySqlExecutor sqlExecutor;
        private readonly ILogger logger;

        public class CoreDbQueueEvent : IQueueEvent, ISqlRecord
        {
            public long Id { get; private set; }

            public bool Processed { get; private set; }

            public string DataJson { get; private set; }

            public int DataType { get; private set; }

            public int Version { get; private set; }

            public Task Fill(DbDataReader reader)
            {
                // Id,IsProcessed,DataJson,Version
                this.DataJson = reader.ReadColumn(nameof(this.DataJson), reader.GetString);
                this.DataType = reader.ReadColumn(nameof(this.DataType), reader.GetInt32);
                this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt64);
                this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);
                this.Processed = reader.ReadColumn(nameof(this.Processed), reader.GetBoolean);
                return Task.FromResult(0);
            }
        }

        public CoreDbTableQueue(string queueTable, MySqlExecutor sqlExecutor, ILoggerFactory loggerFactory)
        {
            if (string.IsNullOrEmpty(queueTable))
            {
                throw new ArgumentNullException(nameof(queueTable));
            }

            this.queueTable = queueTable;
            this.sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
            this.logger = loggerFactory.CreateLogger<CoreDbTableQueue>();
        }

        public async Task<IQueueEvent> Pop()
        {
            for (int i = 0; i < Constants.MaxCoreQueueFetchRetries; i++)
            {
                IList<CoreDbQueueEvent> events = await this.sqlExecutor.QueryAll<CoreDbQueueEvent>(
                    string.Format(CoreQueueQueries.QueueQueryReadyEvent, this.queueTable, Constants.CoreQueueDbEventRetryIntervalMins));
                if (events.Count == 0)
                {
                    return null;
                }

                if (events.Count != 1)
                {
                    throw new DataException($"Unexpected {events.Count} events queried from queue table at once.");
                }

                CoreDbQueueEvent queueEvent = events.Single();
                this.logger.LogDebug("Queue event {0} is selected, updating version to reserve...", queueEvent.Id);
                (int affected, long _) = await this.sqlExecutor.Execute(
                    string.Format(CoreQueueQueries.QueueReserveEvent, this.queueTable),
                    new Dictionary<string, object>
                    {
                        { "@id", queueEvent.Id },
                        { "@version", queueEvent.Version }
                    });

                if (affected >= 1)
                {
                    return queueEvent;
                }

                this.logger.LogDebug("Queue event {0} has been updated by another worker.", queueEvent.Id);
                await Task.Yield();
            }

            return null;
        }

        public async Task Dequeue(IQueueEvent queueEvent)
        {
            if (queueEvent is CoreDbQueueEvent dbEvent)
            {
                (int affected, long _) = await this.sqlExecutor.Execute(
                    string.Format(CoreQueueQueries.QueueDeleteEvent, this.queueTable),
                    new Dictionary<string, object>
                    {
                        { "@id", dbEvent.Id },
                        { "@version", dbEvent.Version }
                    });

                if (affected < 1)
                {
                    throw new DataException($"Unable to delete queue event {dbEvent.Id}");
                }
            }
            else
            {
                throw new ArgumentException($"Unexpected event type {queueEvent.GetType().Name}");
            }
        }

        public async Task PushOne(int eventType, string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            (int _, long lastId) = await this.sqlExecutor.Execute(
                string.Format(CoreQueueQueries.QueueNewEvent, this.queueTable),
                new Dictionary<string, object>
                {
                    { "@dataJson", eventData },
                    { "@dataType", eventType }
                });

            this.logger.LogDebug("New queue event inserted with ID '{0}'", lastId);
        }
    }
}

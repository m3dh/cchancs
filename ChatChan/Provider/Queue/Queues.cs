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

    using Newtonsoft.Json;

    public interface IQueueEvent<out TData> where TData : class, new()
    {
        bool Processed { get; }
        TData Data { get; }
    }

    public interface IMessageQueue<TData> where TData : class, new()
    {
        Task<IQueueEvent<TData>> Pop();
        Task Dequeue(IQueueEvent<TData> queueEvent);
        Task PushOne(TData eventData);
    }

    public class CoreDbTableQueue<TData> : IMessageQueue<TData>
        where TData : class, new()
    {
        private readonly string queueTable;
        private readonly MySqlExecutor sqlExecutor;
        private readonly ILogger logger;

        public class CoreDbQueueEvent : IQueueEvent<TData>, ISqlRecord
        {
            public long Id { get; private set; }

            public bool Processed { get; private set; }

            public TData Data { get; private set; }

            public int Version { get; private set; }

            public Task Fill(DbDataReader reader)
            {
                // Id,IsProcessed,DataJson,Version
                string dataJson = reader.ReadColumn("DataJson", reader.GetString);
                this.Id = reader.ReadColumn(nameof(this.Id), reader.GetInt64);
                this.Version = reader.ReadColumn(nameof(this.Version), reader.GetInt32);
                this.Processed = reader.ReadColumn(nameof(this.Processed), reader.GetBoolean);
                this.Data = JsonConvert.DeserializeObject<TData>(dataJson);
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
            this.logger = loggerFactory.CreateLogger<CoreDbTableQueue<TData>>();
        }

        public async Task<IQueueEvent<TData>> Pop()
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
            }

            return null;
        }

        public Task Dequeue(IQueueEvent<TData> queueEvent)
        {
            throw new NotImplementedException();
        }

        public async Task PushOne(TData eventData)
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            string dataJson = JsonConvert.SerializeObject(eventData);
            (int _, long lastId) = await this.sqlExecutor.Execute(
                string.Format(CoreQueueQueries.QueueNewEvent, this.queueTable),
                new Dictionary<string, object>
                {
                    { "@dataJson", dataJson },
                });

            this.logger.LogDebug("New queue event inserted with ID '{0}'", lastId);
        }
    }
}

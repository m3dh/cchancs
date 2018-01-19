namespace ChatChan.Provider.Partition
{
    using System;
    using System.Collections.Generic;

    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class DataPartitionsManager : IDataPartitionProvider
    {
        private readonly StorageSection storageSection;
        private readonly Random randomer = new Random(DateTimeOffset.UtcNow.Millisecond);
        private readonly IList<Lazy<MySqlExecutor>> partitionExecutors;

        public DataPartitionsManager(IOptions<StorageSection> storageSection, ILoggerFactory loggerFactory)
        {
            this.storageSection = storageSection?.Value ?? throw new ArgumentNullException(nameof(storageSection));
            this.partitionExecutors = new Lazy<MySqlExecutor>[this.storageSection.PartitionCount];

            Lazy<MySqlExecutor> coreDatabase = new Lazy<MySqlExecutor>(() => new MySqlExecutor(this.storageSection.CoreDatabase, loggerFactory));
            foreach (int partition in this.storageSection.CoreDatabase.PartitionKeys)
            {
                if (partition > this.storageSection.PartitionCount)
                {
                    throw new InvalidOperationException($"Invalid core partition {partition}");
                }

                if (null != this.partitionExecutors[partition - 1])
                {
                    throw new InvalidOperationException($"Duplicate core partition {partition}");
                }

                this.partitionExecutors[partition - 1] = coreDatabase;
            }

            if (this.storageSection.DataDatabases != null)
            {
                foreach (MySqlDbSection partitionSection in this.storageSection.DataDatabases)
                {
                    Lazy<MySqlExecutor> dataDatabase = new Lazy<MySqlExecutor>(() => new MySqlExecutor(partitionSection, loggerFactory));
                    partitionSection.PartitionKeys.ForEach(k =>
                    {
                        if (k > this.storageSection.PartitionCount)
                        {
                            throw new InvalidOperationException($"Invalid data partition {k}");
                        }

                        if (null != this.partitionExecutors[k - 1])
                        {
                            throw new InvalidOperationException($"Duplicate data partition {k}");
                        }

                        this.partitionExecutors[k - 1] = dataDatabase;
                    });
                }
            }
        }

        public MySqlExecutor GetDataExecutor(int partition)
        {
            return this.partitionExecutors[partition - 1].Value;
        }

        public int GetPartition()
        {
            // Phase 1 : Just randomly select the partition ID.
            return this.randomer.Next() % this.storageSection.PartitionCount + 1;
        }
    }
}

namespace ChatChan.Provider.Partition
{
    using System;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;
    using Microsoft.Extensions.Options;

    public interface IDataPartitionProvider
    {
        MySqlExecutor GetDataExecutor(int partition);

        int GetPartition();
    }

    public class DataPartitionsManager : IDataPartitionProvider
    {
        private readonly StorageSection storageSection;

        public DataPartitionsManager(IOptions<StorageSection> storageSection)
        {
            this.storageSection = storageSection?.Value ?? throw new ArgumentNullException(nameof(storageSection));
        }

        public MySqlExecutor GetDataExecutor(int partition)
        {
            return null;
        }

        public int GetPartition()
        {
            return 0;
        }
    }
}

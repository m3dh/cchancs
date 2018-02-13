namespace ChatChan.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;
    using ChatChan.Provider.Queue;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public interface IDataPartitionProvider
    {
        MySqlExecutor GetDataExecutor(int partition);
        int GetPartition();
    }

    public class CoreDbProvider : MySqlExecutor
    {
        public CoreDbProvider(ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection, IOptions<StringsSection> stringsSection)
            : base(storageSection?.Value?.CoreDatabase ?? throw new ArgumentNullException(nameof(storageSection)),
                stringsSection?.Value ?? throw new ArgumentNullException(nameof(stringsSection)),
                loggerFactory)
        {
        }
    }

    public class MessageQueueProvider
    {
        private readonly IMessageQueue innerQueue;
        private static readonly Dictionary<string, TaskCompletionSource<bool>> LocalReadiness = new Dictionary<string, TaskCompletionSource<bool>>();

        public MessageQueueProvider(CoreDbProvider coreDb, ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection, IOptions<StringsSection> stringsSection)
        {
            if (storageSection?.Value == null)
            {
                throw new ArgumentNullException(nameof(storageSection));
            }

            if(stringsSection?.Value == null)
            {
                throw new ArgumentNullException(nameof(stringsSection));
            }

            if (string.Equals(Constants.StorageDeployModeAllInOne, storageSection.Value.DeployMode, StringComparison.OrdinalIgnoreCase))
            {
                // use core DB for all in 1 mode.
                this.innerQueue = new CoreDbTableQueue(Constants.ChatAppDbMessageQueueTableName, coreDb, loggerFactory);
            }
            else
            {
                throw new ArgumentException($"Unknown DB deployment mode : {storageSection.Value.DeployMode}");
            }
        }

        public Task<IQueueEvent> Pop()
        {
            return this.innerQueue.Pop();
        }

        public Task<bool> GetLocalReadiness(string threadSignature)
        {
            lock (LocalReadiness)
            {
                if (!LocalReadiness.TryGetValue(threadSignature, out TaskCompletionSource<bool> completionSource)
                    || completionSource.Task.IsCompleted)
                {
                    completionSource = new TaskCompletionSource<bool>();
                    LocalReadiness[threadSignature] = completionSource;
                }

                return completionSource.Task;
            }
        }

        public Task Dequeue(IQueueEvent queueEvent)
        {
            return this.innerQueue.Dequeue(queueEvent);
        }

        public async Task PushOne(int eventType, string eventData)
        {
            await this.innerQueue.PushOne(eventType, eventData);
            lock (LocalReadiness)
            {
                TaskCompletionSource<bool> completionSource = LocalReadiness.Values.FirstOrDefault(c => !c.Task.IsCompleted);
                completionSource?.TrySetResult(true);
            }
        }
    }

    public static class DbProviderHelper
    {
        public static async Task<Tuple<int, long>> RetryOplockUpdate(Func<Task<Tuple<int, long>>> update)
        {
            for (int i = 0; i < Constants.MaxAllowedOpLockRetries; i++)
            {
                (int affect, long lastId) = await update();
                if (affect > 0)
                {
                    return Tuple.Create(affect, lastId);
                }
            }

            // Use 503 error to indicate this is a retriable server internal error.
            throw new ServiceUnavailable("The update operation could not be done, please retry later.");
        }
    }
}
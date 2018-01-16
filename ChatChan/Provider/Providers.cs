namespace ChatChan.Provider
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;
    using ChatChan.Provider.Queue;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class CoreDbProvider : MySqlExecutor
    {
        public CoreDbProvider(ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection)
            : base(storageSection?.Value?.CoreDatabase ?? throw new ArgumentNullException(nameof(storageSection)), loggerFactory)
        {
        }
    }

    public class SendChatMessageQueueProvider : IMessageQueue<SendMessageQueueEvent>
    {
        private readonly IMessageQueue<SendMessageQueueEvent> innerQueue;

        public SendChatMessageQueueProvider(CoreDbProvider coreDb, ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection)
        {
            if (storageSection?.Value == null)
            {
                throw new ArgumentNullException(nameof(storageSection));
            }

            if (string.Equals(Constants.StorageDeployModeAllInOne, storageSection.Value.DeployMode, StringComparison.OrdinalIgnoreCase))
            {
                // use core DB for all in 1 mode.
                this.innerQueue = new CoreDbTableQueue<SendMessageQueueEvent>(Constants.MessageCoreDbTableName, coreDb, loggerFactory);
            }
            else
            {
                throw new ArgumentException($"Unknown DB deployment mode : {storageSection.Value.DeployMode}");
            }
        }

        public Task<IQueueEvent<SendMessageQueueEvent>> Pop()
        {
            return this.innerQueue.Pop();
        }

        public Task Dequeue(IQueueEvent<SendMessageQueueEvent> queueEvent)
        {
            return this.innerQueue.Dequeue(queueEvent);
        }

        public Task PushOne(SendMessageQueueEvent eventData)
        {
            return this.innerQueue.PushOne(eventData);
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
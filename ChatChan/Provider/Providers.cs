namespace ChatChan.Provider
{
    using System;
    using System.Threading.Tasks;
    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider.Executor;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class CoreDbProvider : MySqlExecutor
    {
        public CoreDbProvider(ILoggerFactory loggerFactory, IOptions<StorageSection> storageSection)
            : base(storageSection?.Value?.CoreDatabase ?? throw new ArgumentNullException(nameof(storageSection)), loggerFactory)
        {
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
namespace ChatChan.Tests.UnitTest
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Common.Configuration;
    using ChatChan.Provider;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Xunit;

    public class QueueProviderTests
    {
        [Fact]
        public void SendMessageAndWakeLocalThread()
        {
            var signature = Guid.NewGuid().ToString("N");
            var queue = GetQueueProvider();

            Task readiness = queue.GetLocalReadiness(signature);

            Assert.False(readiness.IsCompleted);
            queue.PushOne(9999, Guid.NewGuid().ToString("N")).Wait();

            Assert.True(readiness.IsCompleted);

            // Do it again!
            readiness = queue.GetLocalReadiness(signature);
            readiness = queue.GetLocalReadiness(signature);

            Assert.False(readiness.IsCompleted);
            queue.PushOne(9999, Guid.NewGuid().ToString("N")).Wait();

            Assert.True(readiness.IsCompleted);
        }

        public static MessageQueueProvider GetQueueProvider()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection);
            MessageQueueProvider queue = new MessageQueueProvider(coreDbProvider, loggerFactory, storageSection);
            return queue;
        }
    }
}

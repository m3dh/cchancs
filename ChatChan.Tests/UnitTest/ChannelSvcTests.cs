namespace ChatChan.Tests.UnitTest
{
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Provider.Partition;
    using ChatChan.Service;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NSubstitute;
    using Xunit;

    public class ChannelSvcTests
    {
        [Fact]
        public void CreateSameOneOnOneChannel_ShallSucceed()
        {
            IChannelService service = GetChannelService();

            var account1 = Mocks.CreateAccount();
            var account2 = Mocks.CreateAccount();
            var channelId = service.CreateDirectMessageChannel(account1, account2, string.Empty).Result;

            Assert.NotNull(channelId);
        }

        private static IChannelService GetChannelService()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IOptions<LimitationsSection> limitationSection = Mocks.GetLimitationSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection);
            IChannelService svc = new ChannelService(loggerFactory, coreDbProvider, partitionProvider);
            return svc;
        }
    }
}

namespace ChatChan.Tests.UnitTest
{
    using ChatChan.Common;
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

            // 0. Create account.
            var account1 = Mocks.CreateAccount();
            var account2 = Mocks.CreateAccount();
            var channelId = service.CreateDirectMessageChannel(account1, account2, string.Empty).Result;

            Assert.NotNull(channelId);

            // 1. Fetch & verify
            var channel = service.GetChannel(channelId).Result;
            Assert.Equal(channelId.Id, channel.Id);
            Assert.Equal(account1.ToString(), channel.OwnerAccountId.ToString());

            // 2. Delete and get.
            service.DeleteChannel(channelId).Wait();
            Assert.ThrowsAsync<NotFound>(async () => { await service.GetChannel(channelId); }).Wait();

            // 3. Recreate & fetch
            var channelId1 = service.CreateDirectMessageChannel(account2, account1, "Some string").Result;
            Assert.Equal(channelId.ToString(), channelId1.ToString());

            channel = service.GetChannel(channelId).Result;
            Assert.Equal(channelId.Id, channel.Id);
            Assert.Equal(account1.ToString(), channel.OwnerAccountId.ToString());

            // 4. Fetch members
            var channelFull = service.GetChannelMembers(channelId1).Result;
            Assert.Equal(2, channelFull.MemberList.Count);
            Assert.Equal(account1.ToString(), channelFull.MemberList[0].ToString());
            Assert.Equal(account2.ToString(), channelFull.MemberList[1].ToString());
        }

        public static IChannelService GetChannelService()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, new OptionsWrapper<StringsSection>(new StringsSection()), loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection, new OptionsWrapper<StringsSection>(new StringsSection()));
            IChannelService svc = new ChannelService(loggerFactory, coreDbProvider, partitionProvider);
            return svc;
        }
    }
}

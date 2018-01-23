namespace ChatChan.Tests.UnitTest
{
    using System;
    using System.Linq;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Provider.Partition;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using NSubstitute;
    using Xunit;

    public class ParticipantSvcTests
    {
        [Fact]
        public void CreateParticipantAndUnlinkAndRelink_ShallSucceed()
        {
            IParticipantService service = GetParticipantService();
            AccountId account = CreateAccount();

            ChannelId channel = new ChannelId
            {
                Id = 1,
                Partition = 1,
                Type = ChannelId.ChannelType.GR
            };

            // Link
            int id = service.LinkAccountWithChannel(account, channel).Result;
            Assert.True(id > 0);

            Participant participant = service.ListAccountParticipants(account).Result.Single();
            Assert.Equal(account.Name, participant.AccountId.Name);
            Assert.Equal(channel.Id, participant.ChannelId.Id);

            // Unlink
            service.UnlinkAccountWithChannel(account, channel).Wait();
            participant = service.ListAccountParticipants(account).Result.SingleOrDefault();
            Assert.Null(participant);

            // Relink
            int id0 = service.LinkAccountWithChannel(account, channel).Result;
            Assert.Equal(id, id0);

            participant = service.ListAccountParticipants(account).Result.Single();
            Assert.Equal(account.Name, participant.AccountId.Name);
            Assert.Equal(channel.Id, participant.ChannelId.Id);
        }

        private static AccountId CreateAccount()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IOptions<LimitationsSection> limitationSection = Mocks.GetLimitationSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection);
            IAccountService accountService = new AccountService(loggerFactory, coreDbProvider, Substitute.For<ITokenService>(), partitionProvider, limitationSection);
            return accountService.CreateUserAccount("Acct-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "Account for participant service tests.").Result;
        }

        private static IParticipantService GetParticipantService()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IOptions<LimitationsSection> limitationSection = Mocks.GetLimitationSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection);
            IAccountService accountService = new AccountService(loggerFactory, coreDbProvider, Substitute.For<ITokenService>(), partitionProvider, limitationSection);
            IParticipantService svc = new ParticipantService(loggerFactory, accountService, partitionProvider);
            return svc;
        }
    }
}

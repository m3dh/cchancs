namespace ChatChan.Tests.UnitTest
{
    using System;
    using System.Collections.Generic;
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
            AccountId account = Mocks.CreateAccount();

            ChannelId channel = new ChannelId
            {
                Id = 1,
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

        [Fact]
        public void SendMessageToCreateParticipant_AndUpdateMessage_ShallSucceed()
        {
            IParticipantService service = GetParticipantService();
            AccountId account0 = Mocks.CreateAccount();
            AccountId account1 = Mocks.CreateAccount();
            ChannelId channel0 = new ChannelId
            {
                Id = 2,
                Type = ChannelId.ChannelType.DM
            };

            // Create participant with message
            bool result = service.UpdateParticipantWithNewMessage(account0, channel0, new Message
            {
                MessageBody = "Lorum ipsum",
                OrdinalNumber = 1,
                Uuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);

            // Fetch with zero time.
            IList<Participant> parts = service.ListAccountParticipantsWithMessageInfo(account0, DateTimeOffset.MinValue).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].LastReadOrdinalNumber);
            Assert.Equal(1, parts[0].LastMessageOrdinalNumber);
            Assert.Equal("Lorum ipsum", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());
            DateTimeOffset lastUpdateDt = parts[0].UpdatedAt;

            // Send another message
            ChannelId channel1 = new ChannelId
            {
                Id = 99,
                Type = ChannelId.ChannelType.GR
            };

            result = service.UpdateParticipantWithNewMessage(account0, channel1, new Message
            {
                MessageBody = "group message",
                OrdinalNumber = 1,
                Uuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, DateTimeOffset.MinValue).Result;
            Assert.Equal(2, parts.Count);

            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastUpdateDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].LastReadOrdinalNumber);
            Assert.Equal(1, parts[0].LastMessageOrdinalNumber);
            Assert.Equal("group message", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());
            lastUpdateDt = parts[0].UpdatedAt;

            // Append one new message.
            result = service.UpdateParticipantWithNewMessage(account0, channel0, new Message
            {
                MessageBody = "Lorum ipsum 1",
                OrdinalNumber = 3,
                Uuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastUpdateDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].LastReadOrdinalNumber);
            Assert.Equal(3, parts[0].LastMessageOrdinalNumber);
            Assert.Equal("Lorum ipsum 1", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());

            // Append an old message.
            result = service.UpdateParticipantWithNewMessage(account0, channel0, new Message
            {
                MessageBody = "Useless message",
                OrdinalNumber = 2,
                Uuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1,
            }).Result;
            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastUpdateDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].LastReadOrdinalNumber);
            Assert.Equal(3, parts[0].LastMessageOrdinalNumber);
            Assert.Equal("Lorum ipsum 1", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());

            // Update read count.
            Assert.True(service.UpdateParticipantLastReadMessageOrdinal(account0, channel0, parts[0].LastMessageOrdinalNumber).Result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastUpdateDt).Result;
            Assert.Equal(3, parts[0].LastReadOrdinalNumber);
        }

        public static IParticipantService GetParticipantService()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IOptions<LimitationsSection> limitationSection = Mocks.GetLimitationSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, new OptionsWrapper<StringsSection>(new StringsSection()), loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection, new OptionsWrapper<StringsSection>(new StringsSection()));
            IAccountService accountService = new AccountService(loggerFactory, coreDbProvider, Substitute.For<ITokenService>(), partitionProvider, limitationSection);
            IParticipantService svc = new ParticipantService(loggerFactory, accountService, partitionProvider);
            return svc;
        }
    }
}

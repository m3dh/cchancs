namespace ChatChan.Tests.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
                MessageText = "Lorum ipsum",
                MessageTsDt = DateTimeOffset.UtcNow,
                MessageUuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);

            // Fetch with zero time.
            IList<Participant> parts = service.ListAccountParticipantsWithMessageInfo(account0, 0).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].MessageRead);
            Assert.Equal(1, parts[0].MessageCount);
            Assert.Equal("Lorum ipsum", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());
            Assert.True(parts[0].LastMessageDt > 0);
            long lastMsgDt = parts[0].LastMessageDt;

            // Send another message
            Thread.Sleep(1);
            ChannelId channel1 = new ChannelId
            {
                Id = 99,
                Type = ChannelId.ChannelType.GR
            };

            result = service.UpdateParticipantWithNewMessage(account0, channel1, new Message
            {
                MessageText = "group message",
                MessageTsDt = DateTimeOffset.UtcNow,
                MessageUuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, 0).Result;
            Assert.Equal(2, parts.Count);

            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastMsgDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].MessageRead);
            Assert.Equal(1, parts[0].MessageCount);
            Assert.Equal("group message", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());
            lastMsgDt = parts[0].LastMessageDt;

            // Append one new message.
            result = service.UpdateParticipantWithNewMessage(account0, channel0, new Message
            {
                MessageText = "Lorum ipsum 1",
                MessageTsDt = DateTimeOffset.UtcNow,
                MessageUuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1
            }).Result;

            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastMsgDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].MessageRead);
            Assert.Equal(2, parts[0].MessageCount);
            Assert.Equal("Lorum ipsum 1", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());

            // Append an old message.
            result = service.UpdateParticipantWithNewMessage(account0, channel0, new Message
            {
                MessageText = "Useless message",
                MessageTsDt = DateTimeOffset.UtcNow.AddMinutes(-10),
                MessageUuid = Guid.NewGuid().ToString("N"),
                SenderAccountId = account1,
            }).Result;
            Assert.True(result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastMsgDt).Result;
            Assert.Equal(1, parts.Count);
            Assert.Equal(0, parts[0].MessageRead);
            Assert.Equal(3, parts[0].MessageCount);
            Assert.Equal("Lorum ipsum 1", parts[0].MessageInfo.MessageFirst100Chars);
            Assert.Equal(account1.ToString(), parts[0].MessageInfo.SenderAccountId.ToString());

            // Update read count.
            Assert.True(service.UpdateParticipantLastReadMessageCount(account0, channel0, parts[0].MessageCount).Result);
            parts = service.ListAccountParticipantsWithMessageInfo(account0, lastMsgDt).Result;
            Assert.Equal(3, parts[0].MessageRead);
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

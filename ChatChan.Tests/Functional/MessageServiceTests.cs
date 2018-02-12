namespace ChatChan.Tests.Functional
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using ChatChan.Controller;
    using Xunit;

    public class MessageServiceTests
    {
        private readonly ChatClient firstChatClient;
        private readonly ChatClient secondChatClient;
        private readonly ChatClient thirdChatClient;

        private readonly string dmChannelId; // Between First & Second

        public MessageServiceTests()
        {
            // Create the first account.
            string accountName = $"MsgTest{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }";
            UserAccountViewModel account = ChatClient.CreateUserAccount(accountName, "Test Account for Messaging");
            Assert.Equal("UA:" + accountName, account.Id);

            string password = $"testP@s$W0rb{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100}";
            ChatClient.SetUserAccountPassword(account.Id, password);

            DeviceTokenViewModel token1 = ChatClient.LogonAccount(accountName, password);
            Assert.False(string.IsNullOrEmpty(token1.Token));

            this.firstChatClient = new ChatClient(account.Id, token1);
            var accounts = this.firstChatClient.SearchUserAccount(accountName.Substring(0, 9));
            Assert.True(accounts.Count <= 10);
            Assert.Contains(accounts, a =>a.Id.Equals("UA:" + accountName, StringComparison.Ordinal));

            // Create second account
            string secondAccountName = $"MsgTestSec{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            account = ChatClient.CreateUserAccount(secondAccountName, "Test Account for Messaging as Second");
            Assert.Equal("UA:" + secondAccountName, account.Id);
            password = $"secsP@s$W0rb{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100}";
            ChatClient.SetUserAccountPassword(account.Id, password);

            DeviceTokenViewModel token2 = ChatClient.LogonAccount(account.Id, password);
            Assert.False(string.IsNullOrEmpty(token2.Token));

            this.secondChatClient = new ChatClient(account.Id, token2);

            // Create third account
            string thirdAccountName = $"MsgTestThd{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            account = ChatClient.CreateUserAccount(thirdAccountName, "Test Account for Messaging as Third");
            Assert.Equal("UA:" + thirdAccountName, account.Id);
            password = $"thdsP@s$W0rb{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100}";
            ChatClient.SetUserAccountPassword(account.Id, password);

            DeviceTokenViewModel token3 = ChatClient.LogonAccount(account.Id, password);
            Assert.False(string.IsNullOrEmpty(token3.Token));

            this.secondChatClient = new ChatClient(account.Id, token3);

            // Create the DM channel in-between.
            GeneralChannelViewModel channel =  this.firstChatClient.CreateDirectMessageChannel(this.secondChatClient.AccountId);
            Assert.True(string.IsNullOrEmpty(channel.DisplayName));

            channel = this.secondChatClient.GetChannel(channel.ChannelId);
            Assert.Equal(2, channel.Members.Count);

            this.dmChannelId = channel.ChannelId;
        }

        [Fact]
        public void DirectMessage_SendNewMessage_CreateMessageUpdates()
        {
            this.firstChatClient.PostNewChannelMessage(this.dmChannelId);
            List<ParticipantViewModel> firstParticipants = this.firstChatClient.ListMyParticipants(0);
            Assert.Single(firstParticipants);
            Assert.Equal(1, firstParticipants[0].LatestMessageOrdinalNumber);
            Assert.Equal(this.dmChannelId, firstParticipants[0].ChannelId);

            List<GeneralMessageViewModel> messages = this.secondChatClient.ListMessagesByChannel(this.dmChannelId);
            Assert.Single(messages);
            Assert.Equal(firstParticipants[0].UnreadLatestMessage, messages[0].MessageBody);

            Thread.Sleep(100);

            // Now get participants as second account.
            List<ParticipantViewModel> secondParticipants = this.secondChatClient.ListMyParticipants(0);
            Assert.Single(secondParticipants);
            Assert.Equal(1, secondParticipants[0].LatestMessageOrdinalNumber);
            Assert.Equal(this.dmChannelId, secondParticipants[0].ChannelId);
        }
    }
}

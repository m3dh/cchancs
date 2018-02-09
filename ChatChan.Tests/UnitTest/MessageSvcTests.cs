namespace ChatChan.Tests.UnitTest
{
    using System;
    using System.Collections.Generic;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Provider.Partition;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Xunit;

    public class MessageSvcTests
    {
        [Fact]
        public void SendMessage_And_Query()
        {
            IMessageService service = MessageSvcTests.GetMessageService();
            IChannelService channel = ChannelSvcTests.GetChannelService();

            AccountId sender = Mocks.CreateAccount();
            AccountId receiver = Mocks.CreateAccount();
            ChannelId channelId = channel.CreateDirectMessageChannel(sender, receiver, string.Empty).Result;

            // Send message and verify.
            string messageGuid = Guid.NewGuid().ToString("N");
            string messageBody = $"Hello from {sender} @ {DateTimeOffset.UtcNow.ToString()}";
            Message msg = service.CreateMessage(channelId, sender, MessageType.Text, messageGuid, messageBody).Result;
            Assert.NotNull(msg);
            Assert.Equal(1, msg.OrdinalNumber);

            IList<Message> messages = service.ListMessages(channelId, 0).Result;
            Assert.Equal(1, messages.Count);
            Assert.Equal(messageBody, messages[0].MessageBody);
            Assert.Equal(messageGuid, messages[0].Uuid);

            Assert.Equal(1, messages[0].OrdinalNumber);

            // Retry message send.
            msg = service.CreateMessage(channelId, sender, MessageType.Text, messageGuid, messageBody).Result;
            Assert.NotNull(msg);
            Assert.Equal(1, msg.OrdinalNumber);

            IList<Message> messages1 = service.ListMessages(channelId, 0).Result;
            Assert.Equal(1, messages1.Count);
            Assert.Equal(messageBody, messages1[0].MessageBody);
            Assert.Equal(messageGuid, messages1[0].Uuid);
            Assert.Equal(1, messages1[0].OrdinalNumber);

            // Send a new message
            string messageGuid1 = Guid.NewGuid().ToString("N");
            string messageBody1 = $"Hello from {sender} @ {DateTimeOffset.UtcNow.ToString()} 2";
            Message msg1 = service.CreateMessage(channelId, sender, MessageType.Text, messageGuid1, messageBody1).Result;
            Assert.NotNull(msg1);
            Assert.Equal(2, msg1.OrdinalNumber);

            IList<Message> messages2 = service.ListMessages(channelId, 1).Result;
            Assert.Equal(1, messages2.Count);
            Assert.Equal(messageBody1, messages2[0].MessageBody);
            Assert.Equal(messageGuid1, messages2[0].Uuid);
            Assert.Equal(2, messages2[0].OrdinalNumber);

            IList<Message> messages3 = service.ListMessages(channelId, 0).Result;
            Assert.Equal(2, messages3.Count);
            Assert.Equal(messageGuid, messages3[0].Uuid);
            Assert.Equal(messageGuid1, messages3[1].Uuid);
        }

        private static IMessageService GetMessageService()
        {
            ILoggerFactory loggerFactory = Mocks.GetLoggerFactory();
            IOptions<StorageSection> storageSection = Mocks.GetStorageSection();
            IDataPartitionProvider partitionProvider = new DataPartitionsManager(storageSection, loggerFactory);
            CoreDbProvider coreDbProvider = new CoreDbProvider(loggerFactory, storageSection);
            IChannelService channelService = new ChannelService(loggerFactory, coreDbProvider, partitionProvider);
            IMessageService svc = new MessageService(loggerFactory, channelService, partitionProvider);
            return svc;
        }
    }
}

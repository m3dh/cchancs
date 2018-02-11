namespace ChatChan.BackendJob
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.Extensions.Logging;

    public class SendChatMessageProcessor : IJobProcessor<SendChatMessageEvent>
    {
        private readonly IMessageService messageService;
        private readonly IChannelService channelService;
        private readonly IParticipantService participantService;
        private readonly ILogger logger;

        public SendChatMessageProcessor(
            ILoggerFactory loggerFactory,
            IMessageService messageService,
            IChannelService channelService,
            IParticipantService participantService)
        {
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            this.participantService = participantService ?? throw new ArgumentNullException(nameof(participantService));
            this.logger = loggerFactory?.CreateLogger<SendChatMessageProcessor>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task<bool> Process(SendChatMessageEvent messageEvent)
        {
            if (messageEvent == null)
            {
                throw new ArgumentNullException(nameof(messageEvent));
            }

            try
            {
                return await this.InnerProcess(messageEvent);
            }
            catch (Exception ex) when (ex is NotFound)
            {
                // Some exceptions are not retryable.
                this.logger.LogError($"Terminating error caught when processing {messageEvent.ChannelId}:{messageEvent.Uuid}, error: {ex}");
                return true;
            }
        }

        private async Task<bool> InnerProcess(SendChatMessageEvent messageEvent)
        {
            this.logger.LogDebug($"Start processing SendEvent for message {messageEvent.Uuid}");
            
            // Read the message and channel members out.
            Message message = await this.messageService.GetMessage(messageEvent.ChannelId, messageEvent.Uuid);
            ChannelMemberList channel = await this.channelService.GetChannelMembers(messageEvent.ChannelId);

            // Loop through the channel members to update.
            List<Task> updateTasks = channel.MemberList
                .Where(act => !act.Equals(message.SenderAccountId))
                .Select(act => this.UpdatePariticipantWithRetry(messageEvent.ChannelId, act, message))
                .ToList();

            await Task.WhenAll(updateTasks);
            return true;
        }

        private async Task UpdatePariticipantWithRetry(ChannelId channel, AccountId account, Message message)
        {
            // 1. Update participants.
            for (int i = 0; i < Constants.MaxAllowedOpLockRetries; i++)
            {
                if (await this.participantService.UpdateParticipantWithNewMessage(account, channel, message))
                {
                    break;
                }
            }

            // 2. Send notification if it's a real message.
            // TODO.
        }
    }
}

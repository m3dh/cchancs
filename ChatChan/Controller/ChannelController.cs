namespace ChatChan.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
    using ChatChan.Middleware;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;

    using Newtonsoft.Json;

    public class DirectMessageChannelInputModel
    {
        [JsonProperty(PropertyName = "source_account_id")]
        public string SourceAccountId { get; set; }

        [JsonProperty(PropertyName = "target_account_id")]
        public string TargetAccountId { get; set; }
    }

    public class PostTextMessageInputModel
    {
        [JsonProperty(PropertyName = "sender_account_id")]
        public string SenderAccountId { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "uuid")]
        public string Uuid { get; set; }
    }

    public class GeneralMessageViewModel
    {
        [JsonProperty(PropertyName = "type")]
        public string MessageType { get; set; }

        [JsonProperty(PropertyName = "message_at")]
        public DateTimeOffset MessageSentAt { get; set; }

        [JsonProperty(PropertyName = "message_body")]
        public string MessageBody { get; set; }

        [JsonProperty(PropertyName = "ordinal")]
        public long OrdinalNumber { get; set; }

        [JsonProperty(PropertyName = "uuid")]
        public string MessageUuid { get; set; }

        [JsonProperty(PropertyName = "sender_id")]
        public string SenderAccountId { get; set; }
    }

    public class GeneralChannelViewModel
    {
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty(PropertyName = "members")]
        public List<string> Members { get; set; }
    }

    public class ChannelController : ControllerBase
    {
        private readonly IChannelService channelService;
        private readonly IAccountService accountService;
        private readonly IParticipantService participantService;
        private readonly IMessageService messageService;
        private readonly IQueueService queueService;
        private readonly LimitationsSection limitations;

        public ChannelController(
            IChannelService channelService,
            IAccountService accountService,
            IParticipantService participantService,
            IMessageService messageService,
            IQueueService queueService,
            IOptions<LimitationsSection> limitations)
        {
            this.channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            this.accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            this.participantService = participantService ?? throw new ArgumentNullException(nameof(participantService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            this.limitations = limitations?.Value ?? throw new ArgumentNullException(nameof(limitations));
        }

        [HttpGet, Route("api/channels/{channelId}")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<GeneralChannelViewModel> GetChannelById(string channelId)
        {
            if (string.IsNullOrEmpty(channelId) || !ChannelId.TryParse(channelId, out ChannelId channelIdObj))
            {
                throw new BadRequest("Channel ID is null or invalid.", nameof(channelId));
            }

            AccountId authAccount = this.GetAuthAccount();
            ChannelMemberList memberList = await this.channelService.GetChannelMembers(channelIdObj);
            if (!memberList.MemberList.Any(act => act.Equals(authAccount)))
            {
                throw new NotAllowed("Cannot read message from a channel that current account is not in.");
            }

            Channel channel = await this.channelService.GetChannel(channelIdObj);
            return new GeneralChannelViewModel
            {
                DisplayName = channel.DisplayName,
                ChannelId = channel.ChannelId.ToString(),
                CreatedAt = channel.CreatedAt,
                Members = memberList.MemberList.Select(m => m.ToString()).ToList()
            };
        }

        [HttpGet, Route("api/channels")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<GeneralChannelViewModel[]> GetChannelsByAccountId([FromQuery] string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest("Unexpected query");
            }

            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Type = AccountId.AccountType.UA, Name = accountId };
            }

            IList<Channel> channels = await this.channelService.GetChannels(accountIdObj);
            return channels.Select(c => new GeneralChannelViewModel
            {
                DisplayName = c.DisplayName,
                ChannelId = c.ChannelId.ToString(),
                CreatedAt = c.CreatedAt,
            }).ToArray();
        }

        [HttpPost, Route("api/channels/dms")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<GeneralChannelViewModel> CreateDirectMessageChannel([FromBody] DirectMessageChannelInputModel input)
        {
            if (input == null)
            {
                throw new BadRequest(nameof(input));
            }

            if (string.IsNullOrEmpty(input.SourceAccountId) || !AccountId.TryParse(input.SourceAccountId, out AccountId sourceAcctId))
            {
                throw new BadRequest(nameof(input.SourceAccountId), input.SourceAccountId);
            }

            if (string.IsNullOrEmpty(input.TargetAccountId) || !AccountId.TryParse(input.TargetAccountId, out AccountId targetAcctId))
            {
                throw new BadRequest(nameof(input.TargetAccountId), input.TargetAccountId);
            }

            if (!this.IsAuthAccount(sourceAcctId))
            {
                throw new Forbidden("Creating channel on behalf of another account is not allowed.");
            }

            UserAccount targetAccount = await this.accountService.GetUserAccount(targetAcctId);
            if (targetAccount == null || string.IsNullOrEmpty(targetAccount.AccountName))
            {
                throw new Forbidden($"Target account {targetAcctId} is not found.");
            }

            ChannelId chanId = await this.channelService.CreateDirectMessageChannel(sourceAcctId, targetAcctId, string.Empty);
            await this.participantService.LinkAccountWithChannel(sourceAcctId, chanId);
            return new GeneralChannelViewModel
            {
                DisplayName = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                ChannelId = chanId.ToString()
            };
        }

        [HttpGet, Route("api/channels/{channelId}/messages")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<GeneralMessageViewModel[]> GetChannelMessages(string channelId, [FromQuery]long lastMsgOrdinalNumber)
        {
            if (string.IsNullOrEmpty(channelId) || !ChannelId.TryParse(channelId, out ChannelId channelIdObj))
            {
                throw new BadRequest("Channel ID is null or invalid.", nameof(channelId));
            }

            AccountId authAccount = this.GetAuthAccount();
            ChannelMemberList memberList = await this.channelService.GetChannelMembers(channelIdObj);
            if(!memberList.MemberList.Any(act => act.Equals(authAccount)))
            {
                throw new NotAllowed("Cannot read message from a channel that current account is not in.");
            }

            IList<Message> msgs = await this.messageService.ListMessages(channelIdObj, lastMsgOrdinalNumber, this.limitations.MaxReturnedMessagesInOneQuery);
            return msgs
                .Select(m => new GeneralMessageViewModel
                {
                    MessageType = m.Type.ToString(),
                    MessageSentAt = m.CreatedAt,
                    MessageBody = m.MessageBody,
                    SenderAccountId = m.SenderAccountId.ToString(),
                    OrdinalNumber = m.OrdinalNumber,
                    MessageUuid = m.Uuid,
                })
                .ToArray();
        }

        [HttpPost, Route("api/channels/{channelId}/textMessages")]
        [ServiceFilter(typeof(TokenAuthActionFilter))]
        public async Task<GeneralMessageViewModel> PostTextMessageInChannel(string channelId, [FromBody] PostTextMessageInputModel messageInput)
        {
            if (string.IsNullOrEmpty(channelId) || !ChannelId.TryParse(channelId, out ChannelId channelIdObj))
            {
                throw new BadRequest("Channel ID is null or invalid.", nameof(channelId));
            }

            if (messageInput == null)
            {
                throw new BadRequest("Request body is null.", nameof(messageInput));
            }

            if(string.IsNullOrEmpty(messageInput.SenderAccountId) || !AccountId.TryParse(messageInput.SenderAccountId, out AccountId senderActId))
            {
                throw new BadRequest("Sender account ID is null or invalid", nameof(messageInput.SenderAccountId));
            }

            if(string.IsNullOrEmpty(messageInput.Uuid) || !Guid.TryParse(messageInput.Uuid, out Guid msgUuid))
            {
                throw new BadRequest("Message UUID is null or invalid", nameof(messageInput.Uuid));
            }

            if(string.IsNullOrEmpty(messageInput.Message) || messageInput.Message.Length > this.limitations.AllowedTextMessageLength)
            {
                throw new BadRequest($"Allowed message length : (0-{this.limitations.AllowedTextMessageLength}]");
            }

            if (!this.IsAuthAccount(senderActId))
            {
                throw new NotAllowed("Cannot send message on behalf of others.");
            }

            AccountId authAccount = this.GetAuthAccount();
            ChannelMemberList memberList = await this.channelService.GetChannelMembers(channelIdObj);
            if (!memberList.MemberList.Any(act => act.Equals(authAccount)))
            {
                throw new NotAllowed("Cannot send message to a channel that current account is not in.");
            }

            Message msg = await this.messageService.CreateMessage(channelIdObj, senderActId, MessageType.Text, msgUuid.ToString("N"), messageInput.Message);
            await this.queueService.SendChatMessage(msg.Uuid, channelIdObj);
            await this.participantService.UpdateParticipantWithNewMessage(senderActId, channelIdObj, msg);
            return new GeneralMessageViewModel
            {
                MessageType = msg.Type.ToString(),
                MessageSentAt = msg.CreatedAt,
                MessageBody = msg.MessageBody,
                SenderAccountId = msg.SenderAccountId.ToString(),
                OrdinalNumber = msg.OrdinalNumber,
                MessageUuid = msg.Uuid,
            };
        }
    }
}

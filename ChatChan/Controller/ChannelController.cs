namespace ChatChan.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Common.Configuration;
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

        [JsonProperty(PropertyName = "message_dt")]
        public long MessageDt { get; set; }

        [JsonProperty(PropertyName = "message_body")]
        public string MessageBody { get; set; }

        [JsonProperty(PropertyName = "sender_id")]
        public string SenderAccountId { get; set; }
    }

    public class GeneralChannelViewModel
    {
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty(PropertyName = "link_id")]
        public int LinkId { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ChannelController : Controller
    {
        private readonly IChannelService channelService;
        private readonly IParticipantService participantService;
        private readonly IMessageService messageService;
        private readonly LimitationsSection limitations;

        public ChannelController(
            IChannelService channelService,
            IParticipantService participantService,
            IMessageService messageService,
            IOptions<LimitationsSection> limitations)
        {
            this.channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            this.participantService = participantService ?? throw new ArgumentNullException(nameof(participantService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.limitations = limitations?.Value ?? throw new ArgumentNullException(nameof(limitations));
        }

        #region Pure Channel APIs
        [HttpGet, Route("api/channels")]
        public async Task<GeneralChannelViewModel[]> GetChannelsByAccountId([FromQuery] string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest("Unexpected query");
            }

            if (string.IsNullOrEmpty(accountId) || !AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                throw new BadRequest(nameof(accountId), accountId);
            }

            IList<Channel> channels = await this.channelService.GetChannels(accountIdObj);
            return channels.Select(c => new GeneralChannelViewModel
            {
                DisplayName = c.DisplayName,
                ChannelId = c.Id.ToString(),
                CreatedAt = c.CreatedAt,
                LinkId = 0,
            }).ToArray();
        }

        [HttpPost, Route("api/channels/dms")]
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

            ChannelId chanId = await this.channelService.CreateDirectMessageChannel(sourceAcctId, targetAcctId, string.Empty);
            int linkId = await this.participantService.LinkAccountWithChannel(sourceAcctId, chanId);
            return new GeneralChannelViewModel
            {
                LinkId = linkId,
                DisplayName = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                ChannelId = chanId.ToString()
            };
        }
        #endregion

        #region Message & Participant APIs
        [HttpGet, Route("api/channels/{channelId}/messages")]
        public async Task<GeneralMessageViewModel[]> GetChannelMessages(string channelId, [FromQuery]long lastMsgDt)
        {
            if (string.IsNullOrEmpty(channelId) || !ChannelId.TryParse(channelId, out ChannelId channelIdObj))
            {
                throw new BadRequest("Channel ID is null or invalid.", nameof(channelId));
            }

            IList<Message> msgs = await this.messageService.ListMessages(channelIdObj, lastMsgDt, this.limitations.MaxReturnedMessagesInOneQuery);
            return msgs
                .Select(m => new GeneralMessageViewModel
                {
                    MessageType = m.Type.ToString(),
                    MessageDt = m.MessageTsDt,
                    MessageBody = m.MessageBody,
                    SenderAccountId = m.SenderAccountId.ToString()
                })
                .ToArray();
        }

        [HttpPost, Route("api/channels/{channelId}/messages")]
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

            Message msg = await this.messageService.CreateMessage(channelIdObj, senderActId, MessageType.Text, msgUuid.ToString("N"), messageInput.Message);
            return new GeneralMessageViewModel
            {
                MessageType = msg.Type.ToString(),
                MessageDt = msg.MessageTsDt,
                MessageBody = msg.MessageBody,
                SenderAccountId = msg.SenderAccountId.ToString(),
            };
        }
        #endregion
    }
}

namespace ChatChan.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;
    using ChatChan.Service.Model;
    using Microsoft.AspNetCore.Mvc;

    using Newtonsoft.Json;

    public class DirectMessageChannelInputModel
    {
        [JsonProperty(PropertyName = "source_account_id")]
        public string SourceAccountId { get; set; }

        [JsonProperty(PropertyName = "target_account_id")]
        public string TargetAccountId { get; set; }
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

        public ChannelController(IChannelService channelService, IParticipantService participantService)
        {
            this.channelService = channelService ?? throw new ArgumentNullException(nameof(channelService));
            this.participantService = participantService ?? throw new ArgumentNullException(nameof(participantService));
        }

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
    }
}

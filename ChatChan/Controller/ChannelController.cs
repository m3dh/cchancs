namespace ChatChan.Controller
{
    using System;
    using System.Threading.Tasks;

    using ChatChan.Common;
    using ChatChan.Service;
    using ChatChan.Service.Identifier;

    using Microsoft.AspNetCore.Mvc;

    using Newtonsoft.Json;

    public class DirectMessageChannelInputModel
    {
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "source_account_id")]
        public string SourceAccountId { get; set; }

        [JsonProperty(PropertyName = "target_account_id")]
        public string TargetAccountId { get; set; }
    }

    public class GeneralChannelViewModel
    {
        [JsonProperty(PropertyName = "link_id")]
        public int LinkId { get; set; }

        [JsonProperty(PropertyName = "channel_id")]
        public string ChannelId { get; set; }
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

        [HttpPost, Route("api/channels/dms")]
        public async Task<GeneralChannelViewModel> CreateDirectMessageChannel([FromBody] DirectMessageChannelInputModel input)
        {
            if (input == null)
            {
                throw new BadRequest(nameof(input));
            }

            if (string.IsNullOrEmpty(input.DisplayName))
            {
                throw new BadRequest(nameof(input.DisplayName));
            }

            if (string.IsNullOrEmpty(input.SourceAccountId) || !AccountId.TryParse(input.SourceAccountId, out AccountId sourceAcctId))
            {
                throw new BadRequest(nameof(input.SourceAccountId), input.SourceAccountId);
            }

            if (string.IsNullOrEmpty(input.TargetAccountId) || !AccountId.TryParse(input.TargetAccountId, out AccountId targetAcctId))
            {
                throw new BadRequest(nameof(input.TargetAccountId), input.TargetAccountId);
            }

            if (!string.Equals(
                sourceAcctId.ToString(),
                this.HttpContext.Items[Constants.HttpContextRealUserNameKey].ToString(),
                StringComparison.OrdinalIgnoreCase))
            {
                throw new Forbidden("Creating channel on behalf of another account is not allowed.");
            }

            ChannelId chanId = await this.channelService.CreateDirectMessageChannel(sourceAcctId, targetAcctId, input.DisplayName);
            int linkId = await this.participantService.LinkAccountWithChannel(sourceAcctId, chanId);
            return new GeneralChannelViewModel
            {
                LinkId = linkId,
                ChannelId = chanId.ToString()
            };
        }
    }
}

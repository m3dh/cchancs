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

    public class ParticipantViewModel
    {
        [JsonProperty(PropertyName = "channel_id")]
        public string ChannelId { get; set; }

        [JsonProperty(PropertyName = "last_ordinal")]
        public long LatestMessageOrdinalNumber { get; set; }

        [JsonProperty(PropertyName = "read_ordinal")]
        public long LastestReadMessageOrdinalNumber { get; set; }

        [JsonProperty(PropertyName = "last_msg")]
        public string UnreadLatestMessage { get; set; }

        [JsonProperty(PropertyName = "last_by")]
        public string UnreadLastMessageBy { get; set; }

        [JsonProperty(PropertyName = "last_sent_at")]
        public DateTimeOffset UnreadLastMessageSentAt { get; set; }

        [JsonProperty(PropertyName = "updated_at")]
        public long UpdatedAtDt { get; set; }
    }

    public class ParticipantController : ControllerBase
    {
        private readonly IParticipantService participantService;
        private readonly IMessageService messageService;
        private readonly LimitationsSection limitations;

        public ParticipantController(
            IParticipantService participantService,
            IMessageService messageService,
            IOptions<LimitationsSection> limitations)
        {
            this.participantService = participantService ?? throw new ArgumentNullException(nameof(participantService));
            this.messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            this.limitations = limitations?.Value ?? throw new ArgumentNullException(nameof(limitations));
        }

        [HttpGet, Route("api/participants")]
        public async Task<ParticipantViewModel[]> GetParticipantsByAccountId([FromQuery] string accountId, [FromQuery]long prevUpdatedDt)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                throw new BadRequest("Unexpected query");
            }

            if (!AccountId.TryParse(accountId, out AccountId accountIdObj))
            {
                accountIdObj = new AccountId { Type = AccountId.AccountType.UA, Name = accountId };
            }

            if (!this.IsAuthAccount(accountIdObj))
            {
                throw new NotAllowed("Querying channel updates for another account is not allowed");
            }

            // Read from prev updated and ensure it's UTC.
            DateTimeOffset prevUpdated = DateTimeOffset.FromUnixTimeMilliseconds(prevUpdatedDt).ToUniversalTime();
            IList<Participant> participants = await this.participantService.ListAccountParticipantsWithMessageInfo(accountIdObj, prevUpdated);
            return participants
                .Select(p => new ParticipantViewModel
                {
                    ChannelId = p.ChannelId.ToString(),
                    LastestReadMessageOrdinalNumber =  p.LastReadOrdinalNumber,
                    LatestMessageOrdinalNumber = p.LastMessageOrdinalNumber,

                    UnreadLastMessageSentAt = p.MessageInfo.MessageCreatedAt,
                    UnreadLastMessageBy = p.MessageInfo.SenderAccountId.ToString(),
                    UnreadLatestMessage = p.MessageInfo.MessageFirst100Chars,

                    UpdatedAtDt = p.UpdatedAt.ToUnixTimeMilliseconds()
                })
                .ToArray();
        }
    }
}

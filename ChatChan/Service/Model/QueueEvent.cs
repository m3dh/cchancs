namespace ChatChan.Service.Model
{
    using ChatChan.Service.Identifier;

    using Newtonsoft.Json;

    public enum ChatAppQueueEventTypes
    {
        SendMessage = 0,

        Ignore = 9999,
    }

    public class SendChatMessageEvent
    {
        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("channel")]
        public ChannelId ChannelId { get; set; }
    }
}

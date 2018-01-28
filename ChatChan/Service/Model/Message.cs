namespace ChatChan.Service.Model
{
    using System;
    using ChatChan.Service.Identifier;

    public class Message
    {
        public string MessageUuid { get; set; }

        public DateTimeOffset MessageTsDt { get; set; }

        public string MessageText { get; set; }

        public AccountId SenderAccountId { get; set; }

        public string GetFirst100MessageChars()
        {
            if (string.IsNullOrEmpty(this.MessageText))
            {
                return null;
            }
            else if (this.MessageText.Length > 100)
            {
                return this.MessageText.Substring(0, 100);
            }
            else
            {
                return this.MessageText;
            }
        }
    }
}

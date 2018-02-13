namespace ChatChan.Common.Configuration
{
    using System.Collections.Generic;

    public class LimitationsSection
    {
        public int AllowedSetAccountPaswordIntervalSecs { get; set; }
        public int AllowedSingleUserDeviceCount { get; set; }
        public int AllowedTextMessageLength { get; set; }

        public int MaxReturnedMessagesInOneQuery { get; set; }
        public int MaxReturnedSearchItemsInQuery { get; set; }

        public double UserAccountDeviceTokenSlidingExpireInHours { get; set; }
        public double UserAccountDeviceTokenAbsoluteExpireInHours { get; set; }
    }

    public class StringsSection : Dictionary<string, string>
    {
    }
}

namespace ChatChan.Common.Configuration
{
    public class LimitationsSection
    {
        public int AllowedSetAccountPaswordIntervalSecs { get; set; }
        public int AllowedSingleUserDeviceCount { get; set; }

        public double UserAccountDeviceTokenSlidingExpireInHours { get; set; }
        public double UserAccountDeviceTokenAbsoluteExpireInHours { get; set; }
    }
}

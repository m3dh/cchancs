namespace ChatChan.Common
{
    public static class Constants
    {
        public const int MaxAllowedOpLockRetries = 5;
        public const int MaxCoreQueueFetchRetries = 3;
        public const int CoreQueueDbEventRetryIntervalMins = 1;

        public static readonly string HttpContextRealUserNameKey = "_context_real_user_name_";

        public static readonly string UserHeaderName = "X-Cchan-UserId";
        public static readonly string TokenHeaderName = "X-Cchan-Token";

        public static readonly string StorageDeployModeAllInOne = "AllInOne";
        public static readonly string MessageCoreDbTableName = "_chat_queue";

    }
}

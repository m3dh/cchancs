namespace ChatChan
{
    using ChatChan.Provider;

    using Microsoft.Extensions.Logging;

    public static class Services
    {
        private static ILoggerFactory loggerFactory;

        public static MySqlProvider CoreDatabase { get; private set; }

        public static void Initialize(ILoggerFactory loggerFactory)
        {
            Services.loggerFactory = loggerFactory;
        }
    }
}

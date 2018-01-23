namespace ChatChan.Tests.UnitTest
{
    using System.Collections.Generic;
    using ChatChan.Common.Configuration;
    using ChatChan.Provider;
    using ChatChan.Provider.Partition;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Console;
    using Microsoft.Extensions.Options;

    public static class Mocks
    {
        private class LoggerFactory : ILoggerFactory
        {
            public void Dispose()
            {
            }

            public ILogger CreateLogger(string categoryName)
            {
                return new ConsoleLogger(categoryName, (s, level) => true, true);
            }

            public void AddProvider(ILoggerProvider provider)
            {
            }
        }

        public static ILoggerFactory GetLoggerFactory()
        {
            return new LoggerFactory();
        }

        public static IOptions<LimitationsSection> GetLimitationSection()
        {
            return new OptionsWrapper<LimitationsSection>(new LimitationsSection());
        }

        public static IOptions<StorageSection> GetStorageSection()
        {
            // Return test database settings.
            return new OptionsWrapper<StorageSection>(new StorageSection
            {
                DeployMode = "AllInOne",
                PartitionCount = 1,
                CoreDatabase = new MySqlDbSection
                {
                    Mode = "MySQL-AutoCommit",
                    DbName = "cchan_core",
                    Server = "localhost",
                    Port = 3306,
                    Uid = "cchan_svc",
                    Password = "T%nt0wn",
                },
                DataDatabases = new[]
                {
                    new MySqlDbSection
                    {
                        Mode = "MySQL-AutoCommit",
                        DbName = "cchan_data",
                        Server = "localhost",
                        Port = 3306,
                        Uid = "cchan_svc",
                        Password = "T%nt0wn",
                        PartitionKeys = new List<int> { 1 }
                    }
                }
            });
        }
    }
}

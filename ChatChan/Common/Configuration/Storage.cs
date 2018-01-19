namespace ChatChan.Common.Configuration
{
    using System.Collections.Generic;

    public class MySqlDbSection
    {
        public string Mode { get; set; }

        public string Server { get; set; }

        public uint Port { get; set; }

        public string DbName { get; set; }

        public string Uid { get; set; }

        public string Password { get; set; }

        public List<int> PartitionKeys { get; set; }
    }

    public class StorageSection
    {
        public string DeployMode { get; set; }

        public int PartitionCount { get; set; }

        public MySqlDbSection CoreDatabase { get; set; }

        public MySqlDbSection[] DataDatabases { get; set; }
    }
}

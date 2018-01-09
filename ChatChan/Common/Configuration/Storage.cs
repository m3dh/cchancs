namespace ChatChan.Common.Configuration
{
    public class MySqlDbSection
    {
        public string Mode { get; set; }

        public string Server { get; set; }

        public uint Port { get; set; }

        public string DbName { get; set; }

        public string Uid { get; set; }

        public string Password { get; set; }
    }

    public class StorageSection
    {
        public string DeployMode { get; set; }

        public MySqlDbSection CoreDatabase { get; set; }
    }
}

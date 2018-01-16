namespace ChatChan.Provider
{
    internal static class CoreQueueQueries
    {
        public const string QueueNewEvent = "INSERT INTO {0}(DataJson) VALUES(@dataJson)";

        public const string QueueQueryReadyEvent =
            "SELECT Id,IsProcessed,DataJson,Version FROM {0} WHERE Version = 0 OR UpdatedAt < CURRENT_TIMESTAMP - INTERVAL {1} MINUTE LIMIT 1";

        public const string QueueReserveEvent =
            "UPDATE {0} SET Version = Version + 1 WHERE Id = @id AND Version = @version";
    }

    internal static class ImageQueries
    {
        public const string CoreImageCreation = "INSERT INTO _images(Uuid, Type, Data) VALUES(@uuid,@type,@data)";

        public const string CoreImageMetaQueryByUuid =
            "SELECT Id, Type, CreatedAt FROM _images WHERE Uuid = @uuid AND IsDeleted = 0";

        public const string CoreImageQueryByUuid = "SELECT Type, Data, CreatedAt FROM _images WHERE Uuid = @uuid AND IsDeleted = 0";
    }

    internal static class AccountQueries
    {
        public const string UserAccountQueryCount = "SELECT COUNT(0) AS Count FROM accounts";

        public const string UserAccountQueryById =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE Id = @id AND IsDeleted = 0";

        public const string UserAccountQueryByAccountName =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE AccountName = @name AND IsDeleted = 0";

        public const string UserAccountCreation = "INSERT INTO accounts(AccountName, DisplayName, Status) VALUES(@name,@display_name,@status)";

        public const string UserAccountUpdatePassword =
            "UPDATE accounts SET Password = @passwd, Status = @new_status, Version = Version + 1 WHERE Id = @id AND Version = @version";

        public const string UserAccountUpdateElseById =
            "UPDATE accounts SET Avatar = @avatar, DisplayName = @display_name, Version = Version + 1 WHERE Id = @id AND Version = @version";
    }

    internal static class TokenQueries
    {
        public const string TokenQueryByAccountName =
            "SELECT Id, AccountName, DeviceId, Token, LastGetAt, ExpiredAt, Version FROM account_tokens WHERE AccountName = @account_name";

        public const string TokenQueryByAccountNameAndDevice =
            "SELECT Id, AccountName, DeviceId, Token, LastGetAt, ExpiredAt, Version FROM account_tokens WHERE AccountName = @account_name AND DeviceId = @device";

        public const string TokenRefresh = "UPDATE account_tokens SET Token = @token, LastGetAt = @last_get, ExpiredAt = @expire, Version = Version + 1 " +
                                           "WHERE Id = @id";

        public const string TokenRefetch = "UPDATE account_tokens SET LastGetAt = @last_get WHERE Id = @id";

        public const string TokenCreateNew = "INSERT INTO account_tokens(AccountName, DeviceId, Token, LastGetAt, ExpiredAt) " +
                                             "VALUES(@account_name,@device,@token,@last_get,@expire)";
    }
}
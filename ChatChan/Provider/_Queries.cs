namespace ChatChan.Provider
{
    internal static class ImageQueries
    {
        // Create core images
        public const string CoreImageCreation = "INSERT INTO _images(Uuid, Type, Data) VALUES(@uuid,@type,@data)";

        // Query core images by UUID and get metadata only.
        public const string CoreImageMetaQueryByUuid =
            "SELECT Id, Type, CreatedAt FROM _images WHERE Uuid = @uuid";

        // Query images by image UUID
        public const string CoreImageQueryByUuid = "SELECT Type, Data, CreatedAt FROM _images WHERE Uuid = @uuid";
    }

    internal static class AccountQueries
    {
        // Counts the user account table.
        public const string UserAccountQueryCount = "SELECT COUNT(0) AS Count FROM accounts";

        // Query user account table by account IDs.
        public const string UserAccountQueryById =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE Id = @id";

        // Query user account table by account names.
        public const string UserAccountQueryByAccountName =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE AccountName = @name";

        // Create user accounts.
        public const string UserAccountCreation = "INSERT INTO accounts(AccountName, DisplayName, Status) VALUES(@name,@display_name,@status)";

        // Update the user account password and status.
        public const string UserAccountUpdatePassword =
            "UPDATE accounts SET Password = @passwd, Status = @new_status, Version = Version + 1 WHERE Id = @id AND Version = @version";

        // Update all remaining user account properties, by ID.
        public const string UserAccountUpdateElseById =
            "UPDATE accounts SET Avatar = @avatar, DisplayName = @display_name, Version = Version + 1 WHERE Id = @id AND Version = @version";
    }

    internal static class TokenQueries
    {
        // Get the all tokens under a given user account name
        public const string TokenQueryByAccountName =
            "SELECT Id, AccountName, DeviceId, Token, LastGetAt, ExpiredAt, Version FROM account_tokens WHERE AccountName = @account_name";

        // Get token by account name and device ID.
        public const string TokenQueryByAccountNameAndDevice =
            "SELECT Id, AccountName, DeviceId, Token, LastGetAt, ExpiredAt, Version FROM account_tokens WHERE AccountName = @account_name AND DeviceId = @device";

        // Updates the token record and all relative timestamps
        public const string TokenRefresh = "UPDATE account_tokens SET Token = @token, LastGetAt = @last_get, ExpiredAt = @expire, Version = Version + 1 " +
                                           "WHERE Id = @id";

        // Updates the token last get time only.
        public const string TokenRefetch = "UPDATE account_tokens SET LastGetAt = @last_get WHERE Id = @id";

        // Inserts a new token into the tokens table
        public const string TokenCreateNew = "INSERT INTO account_tokens(AccountName, DeviceId, Token, LastGetAt, ExpiredAt) " +
                                             "VALUES(@account_name,@device,@token,@last_get,@expire)";
    }
}
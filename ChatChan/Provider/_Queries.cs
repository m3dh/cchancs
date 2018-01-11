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
}
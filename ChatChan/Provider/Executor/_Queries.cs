namespace ChatChan.Provider.Executor
{
    internal static class Queries
    {
        /* Image database (which is to be replaced in the future) */
        // Create images
        public const string ImageCreation = "INSERT INTO _images(Uuid, Type, Data) VALUES(@uuid,@type,@data)";

        // Query images by image UUID
        public const string ImageQueryByUuid = "SELECT Type, Data, CreatedAt FROM _images WHERE Uuid = @uuid";

        /* Core user accounts */
        // Counts the account table.
        public const string AccountQueryCount = "SELECT COUNT(0) AS AccountCount FROM accounts";

        // Query account table by account IDs.
        public const string AccountQueryById =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE Id = @id";

        // Query account table by account names.
        public const string AccountQueryByAccountName =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, CreatedAt, UpdatedAt, Version FROM accounts WHERE AccountName = @name";

        // Create user accounts.
        public const string AccountCreation = "INSERT INTO accounts(AccountName, DisplayName, Status) VALUES(@name,@display_name,@status)";

        // Update the account password and status.
        public const string AccountUpdatePassword =
            "UPDATE accounts SET Password = @passwd, Status = @new_status WHERE Id = @id AND Version = @version";

        // Update all remaining account properties, by ID.
        public const string AccountUpdateElseById =
            "UPDATE accounts SET Avatar = @avatar, DisplayName = @display_name WHERE Id = @id AND Version = @version";
    }
}

namespace ChatChan.Provider
{
    internal static class CoreQueueQueries
    {
        public const string QueueNewEvent = "INSERT INTO `{0}`(`DataJson`,`DataType`) VALUES(@dataJson,@dataType)";

        public const string QueueQueryReadyEvent =
            "SELECT `Id`,`IsProcessed`,`DataJson`,`DataType`,`Version` FROM `{0}` WHERE `Version` = 0 OR `UpdatedAt` < CURRENT_TIMESTAMP - INTERVAL {1} MINUTE LIMIT 1";

        public const string QueueReserveEvent =
            "UPDATE `{0}` SET `Version` = `Version` + 1 WHERE `Id` = @id AND `Version` = @version";

        public const string QueueDeleteEvent = "DELETE FROM `{0}` WHERE `Id` = @id AND `Version` = @version";
    }

    internal static class ImageQueries
    {
        public const string CoreImageCreation = "INSERT INTO _images(Uuid, Type, Data) VALUES(@uuid,@type,@data)";

        public const string CoreImageMetaQueryByUuid =
            "SELECT Id, Type, CreatedAt, IsDeleted FROM _images WHERE Uuid = @uuid";

        public const string CoreImageQueryByUuid = "SELECT Type, Data, CreatedAt, IsDeleted FROM _images WHERE Uuid = @uuid";
    }

    internal static class AccountQueries
    {
        public const string UserAccountQueryCount = "SELECT COUNT(0) AS Count FROM accounts";

        public const string UserAccountQueryById =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, `Partition`, CreatedAt, UpdatedAt, Version FROM accounts WHERE Id = @id";

        public const string UserAccountQueryByAccountName =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, `Partition`, CreatedAt, UpdatedAt, Version FROM accounts WHERE AccountName = @name";

        public const string UserAccountCreation = "INSERT INTO accounts(AccountName, DisplayName, Status, `Partition`) VALUES(@name,@display_name,@status,@partition)";

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
                                           "WHERE Version = @version AND Id = @id";

        public const string TokenRefetch = "UPDATE account_tokens SET LastGetAt = @last_get WHERE Id = @id";

        public const string TokenCreateNew = "INSERT INTO account_tokens(AccountName, DeviceId, Token, LastGetAt, ExpiredAt) " +
                                             "VALUES(@account_name,@device,@token,@last_get,@expire)";
    }

    internal static class ChannelQueries
    {
        private const string ChannelSelectionSlim =
            "SELECT `Id`,`Type`,`Partition`,`DisplayName`,`Status`,`OwnerActId`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `channels` ";

        public static readonly string ChannelQueryById = ChannelSelectionSlim + "WHERE `Id` = @id";

        public static readonly string ChannelQueryMembersById =
            "SELECT `Id`,`Type`,`Status`,`MemberList`,`IsDeleted`,`Version` FROM `channels` WHERE `Id` = @id";

        public static readonly string ChannelCreate =
            "INSERT INTO `channels` (`Type`,`Partition`,`DisplayName`,`Status`,`OwnerActId`,`MemberList`,`MemberHash`)" +
            " VALUES (@type,@partition,@displayName,@status,@ownerActId,@memberList,@memberHash)";

        public static readonly string ChannelCountRecords = "SELECT COUNT(0) FROM `channels`";

        public static readonly string ChannelQueryByOwnerAndMemberHash =
            ChannelSelectionSlim + "WHERE `OwnerActId` = @ownerId AND `MemberHash` = @memberHash";

        public static readonly string ChannelQueryByOwner =
            ChannelSelectionSlim + "WHERE `OwnerActId` = @ownerId";

        public static readonly string ChannelUpdateSoftDelete = "UPDATE `channels` SET `IsDeleted` = @deleted WHERE `Id` = @id";
    }
}
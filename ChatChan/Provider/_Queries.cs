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
            "SELECT Id, Type, CreatedAt FROM _images WHERE Uuid = @uuid AND IsDeleted = 0";

        public const string CoreImageQueryByUuid = "SELECT Type, Data, CreatedAt FROM _images WHERE Uuid = @uuid AND IsDeleted = 0";
    }

    internal static class AccountQueries
    {
        public const string UserAccountQueryCount = "SELECT COUNT(0) AS Count FROM accounts";

        public const string UserAccountQueryById =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, `Partition`, CreatedAt, UpdatedAt, Version FROM accounts WHERE Id = @id AND IsDeleted = 0";

        public const string UserAccountQueryByAccountName =
            "SELECT Id, Password, AccountName, DisplayName, Status, Avatar, `Partition`, CreatedAt, UpdatedAt, Version FROM accounts WHERE AccountName = @name AND IsDeleted = 0";

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
        /*
         *  `Id`           INT          NOT NULL AUTO_INCREMENT,
            `Type`         INT          NOT NULL, -- 1 = 1:1 chat, 2 = In group chat, 3 = Someone to group chat.
            `Partition`    INT          NOT NULL,
            `DisplayName`  VARCHAR(100) NULL,
            `MemberList`   TEXT         NOT NULL,
            `MemberHash`   VARCHAR(45)  NOT NULL,
            `OwnerActId`   VARCHAR(45)  NOT NULL, -- Owner's account ID, for type = 1 chats, this field is not used.
            `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
            `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            `IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
            `Version`      INT          NOT NULL DEFAULT 0,
         */
        private const string ChannelSelectionSlim =
            "SELECT `Id`,`Type`,`Partition`,`DisplayName`,`MemberHash`,`OwerActId`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `channels` ";

        public static readonly string ChannelQueryById = ChannelSelectionSlim + "WHERE `Id` = @id";

        public static readonly string ChannelQueryMembersById = "SELECT `MemberList`,`Version` FROM `channels` WHERE `Id` = @id";
    }

    internal static class ParticipantQueries
    {
        /*
         *      `Id`           INT          NOT NULL AUTO_INCREMENT,
                `AccountId`    VARCHAR(45)  NOT NULL, -- This participant's account ID.
                `ChannelId`    VARCHAR(45)  NOT NULL, -- The other side of this session.
                `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                `IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
                `Version`      INT          NOT NULL DEFAULT 0,
         */
        private const string ParticipantSelection = "SELECT `Id`,`AccountId`,`ChannelId`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `participants` ";

        public static readonly string ParticipantQueryById = ParticipantSelection + "WHERE `Id` = @id";

        public static readonly string ParticipantQueryByAccountId = ParticipantSelection + "WHERE `AccountId` = @accountId";

        public static readonly string ParticipantCreate = "INSERT INTO `participants` (`AccountId`,`ChannelId`) VALUES (@accountId,@channelId)";
    }
}
namespace ChatChan.Provider
{
    internal static class ParticipantQueries
    {
        /*    `Id`           INT
         *    `AccountId`    VARCHAR(45)
         *    `ChannelId`    VARCHAR(45)
         *    `MessageInfo`  TEXT
         *    `MessageCount` INT
         *    `MessageRead`  INT
         *    `CreatedAt`    DATETIME
         *    `UpdatedAt`    DATETIME
         *    `IsDeleted`    TINYINT
         *    `Version`      INT
         */

        private const string ParticipantSelection =
            "SELECT `Id`,`AccountId`,`ChannelId`,`LastMessageDt`,`MessageCount`,`MessageRead`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `participants` ";

        private const string ParticipantSelectionFull =
            "SELECT `Id`,`AccountId`,`ChannelId`,`LastMessageDt`,`MessageInfo`,`MessageCount`,`MessageRead`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `participants` ";

        public static readonly string ParticipantQueryByIds = ParticipantSelection + "WHERE `AccountId` = @accountId AND `ChannelId` = @channelId";

        public static readonly string ParticipantQueryByAccountId = ParticipantSelection + "WHERE `AccountId` = @accountId";

        public static readonly string ParticipantQueryFullByAccountIdAndLastMsgDt = ParticipantSelectionFull + "WHERE `AccountId` = @accountId AND `LastMessageDt` > @lastMsgDt";

        public static readonly string ParticipantCreateWithMessage =
            "INSERT INTO `participants` (`AccountId`,`ChannelId`,`LastMessageDt`,`MessageInfo`,`MessageCount`) VALUES (@accountId,@channelId,@lastMsgDt,@messageInfo,1)";

        public static readonly string ParticipantCreate =
            "INSERT INTO `participants` (`AccountId`,`ChannelId`) VALUES (@accountId,@channelId)";

        public static readonly string ParticipantUpdateSoftDelete =
            "UPDATE `participants` SET `IsDeleted` = @deleted WHERE `AccountId` = @accountId AND `ChannelId` = @channelId";

        public static readonly string ParticipantUpdateMessageInfo =
            "UPDATE `participants` SET `LastMessageDt`=@lastMsgDt,`MessageInfo`=@messageInfo,`MessageCount`=`MessageCount`+1,`Version`=`Version`+1 WHERE `Id`=@id AND `Version`=@version";

        public static readonly string ParticipantUpdateMessageCount = "UPDATE `participants` SET `MessageCount`=`MessageCount`+1 WHERE `Id`=@id AND `Version`=@version";

        public static readonly string ParticipantUpdateLastReadCount = "UPDATE `participants` SET `MessageRead`=@read WHERE `Id`=@id AND `Version`=@version";
    }

    internal static class MessageQueries
    {
        public static readonly string CreateMessage =
            "INSERT INTO `messages` (`Uuid`,`Type`,`MessageBody`,`ChannelId`,`SenderActId`,`MessageDt`) VALUES (@uuid,@type,@body,@channelId,@senderId,@messageDt)";

        public static readonly string QueryMessageByChannelIdAndDt =
            "SELECT `Id`,`Uuid`,`Type`,`MessageBody`,`SenderActId`,`MessageDt` FROM `messages` WHERE `ChannelId`=@channelId AND `MessageDt` > @msgDt ORDER BY `MessageDt` LIMIT @selection";
    }
}

namespace ChatChan.Provider
{
    internal static class ParticipantQueries
    {
        private const string ParticipantSelection =
            "SELECT `Id`,`AccountId`,`ChannelId`,`LastReadOn`,`LastMessageOn`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `participants` ";

        private const string ParticipantSelectionFull =
            "SELECT `Id`,`AccountId`,`ChannelId`,`MessageInfo`,`LastReadOn`,`LastMessageOn`,`CreatedAt`,`UpdatedAt`,`IsDeleted`,`Version` FROM `participants` ";

        public static readonly string ParticipantQueryByIds = ParticipantSelection + "WHERE `AccountId` = @accountId AND `ChannelId` = @channelId";

        public static readonly string ParticipantQueryByAccountId = ParticipantSelection + "WHERE `AccountId` = @accountId";

        public static readonly string ParticipantQueryFullByAccountIdAndLastMsgDt = ParticipantSelectionFull + "WHERE `AccountId` = @accountId AND `UpdatedAt` > @updatedAt";

        public static readonly string ParticipantCreateWithMessage =
            "INSERT INTO `participants` (`AccountId`,`ChannelId`,`MessageInfo`,`LastMessageOn`) VALUES (@accountId,@channelId,@messageInfo,@messageOn)";

        public static readonly string ParticipantCreate =
            "INSERT INTO `participants` (`AccountId`,`ChannelId`) VALUES (@accountId,@channelId)";

        public static readonly string ParticipantUpdateSoftDelete =
            "UPDATE `participants` SET `IsDeleted` = @deleted WHERE `AccountId` = @accountId AND `ChannelId` = @channelId";

        public static readonly string ParticipantUpdateMessageInfo =
            "UPDATE `participants` SET `MessageInfo`=@messageInfo,`LastMessageOn`=@lastMsgOn,`Version`=`Version`+1 WHERE `Id`=@id AND `Version`=@version";

        public static readonly string ParticipantUpdateLastReadOrdinal = "UPDATE `participants` SET `LastReadOn`=@lastReadOn WHERE `Id`=@id AND `LastReadOn`<@lastReadOn";
    }

    internal static class MessageQueries
    {
        public static readonly string GetMessage =
            "SELECT `Id`,`Uuid`,`Type`,`MessageBody`,`SenderActId`,`CreatedAt`,`OrdinalNumber` FROM `messages` WHERE `ChannelId`=@channelId AND `Uuid`=@uuid";

        public static readonly string CreateMessage =
            "INSERT INTO `messages` (`Uuid`,`Type`,`MessageBody`,`ChannelId`,`SenderActId`,`OrdinalNumber`) " +
            "VALUES (@uuid,@type,@body,@channelId,@senderId," +
            "IFNULL((SELECT * FROM (SELECT MAX(`OrdinalNumber`) FROM `messages` WHERE `ChannelId`=@channelId) AS T),0)+1)";

        public static readonly string QueryMessageByChannelIdAndDt =
            "SELECT `Id`,`Uuid`,`Type`,`MessageBody`,`SenderActId`,`CreatedAt`,`OrdinalNumber` " +
            "FROM `messages` WHERE `ChannelId`=@channelId AND `OrdinalNumber` > @ordinalNumber ORDER BY `OrdinalNumber` LIMIT @selection";
    }
}

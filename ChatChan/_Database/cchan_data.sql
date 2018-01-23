CREATE DATABASE IF NOT EXISTS cchan_data CHARACTER SET=UTF8MB4;

USE cchan_data;

CREATE TABLE IF NOT EXISTS `participants` (
    `Id`           INT          NOT NULL AUTO_INCREMENT,
    `AccountId`    VARCHAR(45)  NOT NULL, -- This participant's account ID.
    `ChannelId`    VARCHAR(45)  NOT NULL, -- The other side of this session.
    `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
    `Version`      INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_AccountId_ChannelId` UNIQUE INDEX (`AccountId`, `ChannelId`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `channels` (
    `Id`           INT          NOT NULL AUTO_INCREMENT,
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
	PRIMARY KEY (`Id`),
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `messages` (
	`Id`           BIGINT       NOT NULL AUTO_INCREMENT,
	`Type`         INT          NOT NULL,
	`Message`      TEXT         NOT NULL,
	`ChannelId`    VARCHAR(45)  NOT NULL,
	`SenderActId`  VARCHAR(45)  NOT NULL,
	`Uuid`         VARCHAR(36)  NOT NULL,
    `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
    `Version`      INT          NOT NULL DEFAULT 0,
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `msg_updates` (
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
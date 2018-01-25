CREATE DATABASE IF NOT EXISTS cchan_data CHARACTER SET=UTF8MB4;

USE cchan_data;

-- Participants stored in the same partition by account.
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

-- Message updates are stored per account and is partitioned by accounts.
CREATE TABLE IF NOT EXISTS `msg_updates` (
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

-- Messages are partitioned by channels.
CREATE TABLE IF NOT EXISTS `messages` (
	`Id`           BIGINT       NOT NULL AUTO_INCREMENT,
	`Type`         INT          NOT NULL,
	`Message`      TEXT         NOT NULL,
	`ChannelId`    VARCHAR(45)  NOT NULL,
	`SenderActId`  VARCHAR(45)  NOT NULL,
	`Uuid`         VARCHAR(36)  NOT NULL,
    `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `Version`      INT          NOT NULL DEFAULT 0,
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
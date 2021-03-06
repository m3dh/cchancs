﻿CREATE DATABASE IF NOT EXISTS cchan_data CHARACTER SET=UTF8MB4;

USE cchan_data;

-- Participants stored in the same partition by account.
CREATE TABLE IF NOT EXISTS `participants` (
	`Id`            INT          NOT NULL AUTO_INCREMENT,
	`AccountId`     VARCHAR(45)  NOT NULL, -- This participant's account ID.
	`ChannelId`     VARCHAR(45)  NOT NULL, -- The other side of this session.
	`MessageInfo`   TEXT         NULL,
	`LastReadOn`    BIGINT       NOT NULL DEFAULT 0, -- the last known read message count.
	`LastMessageOn` BIGINT       NOT NULL DEFAULT 0, -- the last message's ordinal number.
	`CreatedAt`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	`UpdatedAt`     DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
	`IsDeleted`     TINYINT      NOT NULL DEFAULT 0,
	`Version`       INT          NOT NULL DEFAULT 0,
	`Status`        BIGINT       NOT NULL DEFAULT 0,
	PRIMARY KEY (`Id`),
	CONSTRAINT `UIX_AccountId_ChannelId` UNIQUE INDEX (`AccountId`, `ChannelId`),
	INDEX `IX_AccountId_UpdatedAt` (`AccountId`,`UpdatedAt`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

-- Messages are partitioned by channels.
CREATE TABLE IF NOT EXISTS `messages` (
	`Id`            BIGINT       NOT NULL AUTO_INCREMENT,
	`Uuid`          VARCHAR(36)  NOT NULL,
	`Type`          INT          NOT NULL,
	`MessageBody`   TEXT         NOT NULL,
	`ChannelId`     VARCHAR(45)  NOT NULL,
	`SenderActId`   VARCHAR(45)  NOT NULL,
	`OrdinalNumber` BIGINT       NOT NULL,
	`CreatedAt`     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
	PRIMARY KEY (`Id`),
	CONSTRAINT `UIX_ChannelId_Uuid` UNIQUE INDEX (`ChannelId`, `Uuid`),
	INDEX `IX_ChannelId_OrdinalNumber` (`ChannelId`,`OrdinalNumber`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
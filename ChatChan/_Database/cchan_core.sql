﻿-- mysql -u username -p database_name < file.sql
-- mysqld --default_time_zone='+00:00' --explicit_defaults_for_timestamp

CREATE DATABASE IF NOT EXISTS cchan_core CHARACTER SET=UTF8MB4;

USE cchan_core;

CREATE TABLE IF NOT EXISTS `accounts` (
    `Id`          INT          NOT NULL AUTO_INCREMENT,
    `Password`    VARCHAR(45)  NULL,
    `AccountName` VARCHAR(40)  NOT NULL,
    `DisplayName` VARCHAR(100) NOT NULL,
    `Status`      BIGINT       NOT NULL DEFAULT 0,
    `Avatar`      VARCHAR(45)  NULL,
    `Partition`   INT          NOT NULL,
    `CreatedAt`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `Version`     INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_AccountName` UNIQUE INDEX (`AccountName`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `account_tokens` (
    `Id`          INT         NOT NULL AUTO_INCREMENT,
    `AccountName` VARCHAR(40) NOT NULL,
    `DeviceId`    INT         NOT NULL,
    `Token`       VARCHAR(45) NOT NULL,
    `LastGetAt`   DATETIME    NOT NULL,
    `ExpiredAt`   DATETIME    NOT NULL,
    `CreatedAt`   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`   DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `Version`     INT         NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_AccountName_DeviceId` UNIQUE INDEX (`AccountName`, `DeviceId`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `channels` (
    `Id`           INT          NOT NULL AUTO_INCREMENT,
    `Type`         INT          NOT NULL, -- 0 = 1:1 chat, 1 = In group chat, 2 = Someone to group chat.
    `Partition`    INT          NOT NULL,
    `DisplayName`  VARCHAR(100) NULL,
    `Status`       BIGINT       NOT NULL DEFAULT 0,
    `MemberList`   TEXT         NOT NULL,
    `MemberHash`   VARCHAR(45)  NOT NULL,
    `OwnerActId`   VARCHAR(45)  NOT NULL, -- Owner's account ID, for type = 1 chats, the owner is the account with a smaller account ID.
    `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
    `Version`      INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_Owner_Members` UNIQUE INDEX (`OwnerActId`, `MemberHash`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `_images` (
    `Id`         INT         NOT NULL AUTO_INCREMENT,
    `Uuid`       VARCHAR(36) NOT NULL,
    `Type`       VARCHAR(10) NOT NULL,
    `Data`       MEDIUMBLOB  NOT NULL,
    `CreatedAt`  DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `IsDeleted`  TINYINT     NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_ImageUuid` UNIQUE INDEX (`Uuid`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `_chat_queue` (
    `Id`          BIGINT     NOT NULL AUTO_INCREMENT,
    `IsProcessed` TINYINT    NOT NULL DEFAULT 0,
    `DataType`    INT        NOT NULL,
    `DataJson`    TEXT       NOT NULL,
    `Version`     INT        NOT NULL DEFAULT 0, -- workflow select version = 0 or updated_at > 1 min ago. (So just update @Version = @Version + 1 to reserve the message 30s for you).
    `UpdatedAt`   DATETIME   NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`Id`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
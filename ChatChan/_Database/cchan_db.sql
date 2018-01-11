-- mysql -u username -p database_name < file.sql
-- mysqld --default_time_zone='+00:00' --explicit_defaults_for_timestamp

CREATE DATABASE IF NOT EXISTS cchan_db CHARACTER SET=UTF8MB4;

USE cchan_db;

CREATE TABLE IF NOT EXISTS `accounts` (
    `Id`          INT          NOT NULL AUTO_INCREMENT,
    `Password`    VARCHAR(45)  NULL,
    `AccountName` VARCHAR(64)  NOT NULL,
    `DisplayName` VARCHAR(256) NOT NULL,
    `Status`      BIGINT       NOT NULL DEFAULT 0,
    `Avatar`      VARCHAR(45)  NULL,
    `CreatedAt`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `Version`     INT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_AccountName` UNIQUE INDEX (`AccountName`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;

CREATE TABLE IF NOT EXISTS `account_tokens` (
    `Id`          INT         NOT NULL AUTO_INCREMENT,
    `AccountName` VARCHAR(64) NOT NULL,
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

CREATE TABLE IF NOT EXISTS `_images` (
    `Id`         INT         NOT NULL AUTO_INCREMENT,
    `Uuid`       VARCHAR(36) NOT NULL,
    `Type`       VARCHAR(10) NOT NULL,
    `Data`       MEDIUMBLOB  NOT NULL,
    `CreatedAt`  DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_ImageUuid` UNIQUE INDEX (`Uuid`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
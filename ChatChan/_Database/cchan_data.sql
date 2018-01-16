CREATE DATABASE IF NOT EXISTS cchan_data CHARACTER SET=UTF8MB4;

USE cchan_data;

CREATE TABLE IF NOT EXISTS `participants` (
    `Id`           BIGINT       NOT NULL AUTO_INCREMENT,
	`Type`         INT          NOT NULL, -- 1 = 1:1 chat, 2 = In group chat, 3 = Someone to group chat. 
	`AccountId`    VARCHAR(45)  NOT NULL, -- The owner of this session, couldn't be a group.
	`Participant1` VARCHAR(45)  NOT NULL, -- The other side of this session.
    `CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	`IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
    `Version`      INT          NOT NULL DEFAULT 0,
	PRIMARY KEY (`Id`),
    CONSTRAINT `UIX_AccountName` UNIQUE INDEX (`AccountName`)
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;


CREATE TABLE IF NOT EXISTS `channels` (
	`Id`           BIGINT       NOT NULL AUTO_INCREMENT,
	`CreatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
	`IsDeleted`    TINYINT      NOT NULL DEFAULT 0,
    `Version`      INT          NOT NULL DEFAULT 0,
) ENGINE = InnoDb DEFAULT CHARSET=UTF8MB4 AUTO_INCREMENT=1;
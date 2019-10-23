USE dependencytest;
IF (NOT EXISTS (select * from sysobjects where name='Messages' and xtype='U'))
BEGIN
CREATE TABLE [dbo].[Messages] (
	[ID] [int] IDENTITY(1, 1) NOT NULL,
	[MessageText] [nvarchar] (255) NOT NULL,
	[EventTime] [datetime] NOT NULL
)
END
GO

INSERT INTO [dbo].[Messages] 
		([MessageText]
		,[EventTime])
	VALUES 
		('Message1', '2017-12-31 00:12:34'),
		('Message2', '2017-12-31 00:12:35'),
		('Message3', '2017-12-31 00:12:36'),
		('Message4', '2017-12-31 00:12:37'),
		('Message5', '2017-12-31 00:12:38'),
		('Message6', '2017-12-31 00:12:39'),
		('Message7', '2017-12-31 00:12:40'),
		('Message8', '2017-12-31 00:12:40'),
		('Message9', '2017-12-31 00:12:40'),
		('Message10', '2017-12-31 00:12:40'),
		('Message11', '2017-12-31 00:12:40'),
		('Message12', '2017-12-31 00:12:40'),
		('Message13', '2017-12-31 00:12:40'),
		('Message14', '2017-12-31 00:12:40'),
		('Message15', '2017-12-31 00:12:40'),
		('Message16', '2017-12-31 00:12:40'),
		('Message17', '2017-12-31 00:12:40'),
		('Message18', '2017-12-31 00:12:40'),
		('Message19', '2017-12-31 00:12:40'),
		('Message12', '2017-12-31 00:12:40'),
		('Message13', '2017-12-31 00:12:40'),
		('Message14', '2017-12-31 00:12:40'),
		('Message15', '2017-12-31 00:12:40'),
		('Message16', '2017-12-31 00:12:40'),
		('Message17', '2017-12-31 00:12:40'),
		('Message18', '2017-12-31 00:12:40'),
		('Message19', '2017-12-31 00:12:40'),
		('Message20', '2017-12-31 00:12:40');
GO


IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetTopTenMessages')
DROP PROCEDURE GetTopTenMessages
GO

CREATE PROCEDURE [dbo].[GetTopTenMessages]
AS
BEGIN
SET NOCOUNT ON;
WAITFOR DELAY '00:00:00:006';
SELECT TOP 10 ID, MessageText, EventTime from Messages
END
GO

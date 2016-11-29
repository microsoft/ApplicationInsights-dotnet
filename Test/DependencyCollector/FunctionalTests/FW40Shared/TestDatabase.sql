
CREATE USER NETWORKSERVICE FOR LOGIN [NT AUTHORITY\NETWORK SERVICE]

GO

CREATE TABLE [dbo].[Messages] (
	[ID] [int] IDENTITY(1, 1) NOT NULL,
	[MessageText] [nvarchar] (255) NOT NULL,
	[EventTime] [datetime] NOT NULL
)
GO

exec sp_addrolemember @rolename='db_datareader', @membername='NETWORKSERVICE'

GO



INSERT INTO [dbo].[Messages] 
		([MessageText]
		,[EventTime])
	VALUES 
		('Message1', '2015-12-31 00:12:34'),
		('Message2', '2015-12-31 00:12:35'),
		('Message3', '2015-12-31 00:12:36'),
		('Message4', '2015-12-31 00:12:37'),
		('Message5', '2015-12-31 00:12:38'),
		('Message6', '2015-12-31 00:12:39'),
		('Message7', '2015-12-31 00:12:40'),
		('Message8', '2015-12-31 00:12:40'),
		('Message9', '2015-12-31 00:12:40'),
		('Message10', '2015-12-31 00:12:40'),
		('Message11', '2015-12-31 00:12:40'),
		('Message12', '2015-12-31 00:12:40'),
		('Message13', '2015-12-31 00:12:40'),
		('Message14', '2015-12-31 00:12:40'),
		('Message15', '2015-12-31 00:12:40'),
		('Message16', '2015-12-31 00:12:40'),
		('Message17', '2015-12-31 00:12:40'),
		('Message18', '2015-12-31 00:12:40'),
		('Message19', '2015-12-31 00:12:40'),
		('Message12', '2015-12-31 00:12:40'),
		('Message13', '2015-12-31 00:12:40'),
		('Message14', '2015-12-31 00:12:40'),
		('Message15', '2015-12-31 00:12:40'),
		('Message16', '2015-12-31 00:12:40'),
		('Message17', '2015-12-31 00:12:40'),
		('Message18', '2015-12-31 00:12:40'),
		('Message19', '2015-12-31 00:12:40'),
		('Message20', '2015-12-31 00:12:40');
GO

CREATE PROCEDURE [dbo].[GetTopTenMessages]

AS
BEGIN

SET NOCOUNT ON;
WAITFOR DELAY '00:00:00:006';
SELECT TOP 10 ID, MessageText, EventTime from Messages

END

GO

grant execute on object::dbo.GetTopTenMessages to NETWORKSERVICE


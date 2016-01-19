
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
		('Message7', '2015-12-31 00:12:40');
GO

CREATE PROCEDURE [dbo].[GetTopTenMessages]

AS
BEGIN

SET NOCOUNT ON;

SELECT TOP 10 ID, MessageText, EventTime from Messages

END

GO

grant execute on object::dbo.GetTopTenMessages to NETWORKSERVICE


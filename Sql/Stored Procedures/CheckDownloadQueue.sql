﻿CREATE PROCEDURE [dbo].[CheckDownloadQueue]
AS
	SELECT COUNT(*) FROM DownloadQueue WHERE datecreated > (SELECT value FROM VarDates WHERE id=1)
	UPDATE VarDates SET value=GETDATE() WHERE id=1

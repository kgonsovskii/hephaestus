use hephaestus
-- Drop the stored procedure if it exists

IF OBJECT_ID('dbo.UpsertBotLog', 'P') IS NOT NULL
BEGIN
DROP PROCEDURE dbo.UpsertBotLog;
END
GO

IF OBJECT_ID('dbo.Clean', 'P') IS NOT NULL
BEGIN
DROP PROCEDURE dbo.Clean;
END
GO


IF OBJECT_ID('dbo.CalcStats', 'P') IS NOT NULL
BEGIN
DROP PROCEDURE dbo.CalcStats;
END
GO

IF OBJECT_ID('dbo.LogDn', 'P') IS NOT NULL
BEGIN
DROP PROCEDURE dbo.LogDn;
END
GO


DROP table if exists dbo.botLog

DROP table if exists dbo.dnLog

CREATE TABLE dbo.botLog (
                            id varchar(100) PRIMARY KEY,
                            server VARCHAR(15),
                            first_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
                            last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
                            first_seen_ip VARCHAR(15),
                            last_seen_ip VARCHAR(15),
                            serie VARCHAR(100),
                            number VARCHAR(100),
                            number_of_requests INT DEFAULT 1,
                            number_of_elevated_requests INT DEFAULT 0,
							number_of_downloads int,
							install_calculated DATETIME,
							uninstall_calculated DATETIME
);
GO


CREATE TABLE dbo.dnLog ( ip VARCHAR(15) PRIMARY KEY,
                           server VARCHAR(15),
                           profile VARCHAR(100),
                           first_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
                           last_seen DATETIME DEFAULT CURRENT_TIMESTAMP,
						   number_of_requests  INT DEFAULT 1,
);
GO

-- Create or alter the stored procedure
CREATE PROCEDURE dbo.UpsertBotLog
    @server VARCHAR(15),
    @ip VARCHAR(15),
    @id VARCHAR(100),
    @elevated INT = 0,
    @serie VARCHAR(100) = NULL,
    @number VARCHAR(100) = NULL ,
    @timeDif int = 0 
AS
BEGIN
	
    -- Use MERGE to handle insert or update
MERGE dbo.botLog AS target
    USING (VALUES (@id, @server, @serie, @number, @ip, @ip, @timeDif))
    AS source (id, server, serie, number, first_seen_ip, last_seen_ip, time_dif)
    ON target.id = source.id
    WHEN MATCHED THEN
UPDATE SET
    last_seen = CURRENT_TIMESTAMP,                  -- Update last seen timestamp
    last_seen_ip = source.last_seen_ip,             -- Update last seen IP address
    number_of_requests = target.number_of_requests + 1,  -- Increment number of requests
    number_of_elevated_requests = target.number_of_elevated_requests + @elevated  -- Increment elevated requests if elevated > 0
    WHEN NOT MATCHED BY TARGET THEN
INSERT (id, server, first_seen, last_seen, first_seen_ip, last_seen_ip, serie, number, number_of_requests, number_of_elevated_requests)
VALUES (
    source.id,                                      -- Use provided @id
    source.server,                                  -- Server name or address
    CURRENT_TIMESTAMP,                              -- First seen timestamp
    CURRENT_TIMESTAMP,                              -- Last seen timestamp
    source.first_seen_ip,                           -- First seen IP address
    source.last_seen_ip,                            -- Last seen IP address
    source.serie,                                   -- Serie (provided during insert)
    source.number,                                  -- Number (provided during insert)
    1,                                              -- Number of requests set to 1 for new record
    @elevated                                       -- Number of elevated requests set to @elevated value for new record
    );
END;
GO

CREATE PROCEDURE dbo.LogDn
    @server VARCHAR(15),
    @profile VARCHAR(100),
    @ip varchar(15)
AS
BEGIN
MERGE dbo.dnLog AS target
    USING (VALUES (@ip, @server, @profile))
    AS source (ip, server, profile)
    ON target.ip = source.ip
    WHEN MATCHED THEN
UPDATE SET
    last_seen = CURRENT_TIMESTAMP,
    number_of_requests = target.number_of_requests + 1
    WHEN NOT MATCHED BY TARGET THEN
INSERT (ip, server, profile, first_seen, last_seen, number_of_requests)
VALUES (
    source.ip,                                      
    source.server,                                  
    source.profile,                              
    CURRENT_TIMESTAMP,                              
    CURRENT_TIMESTAMP,                                          
    1                                                               
    );
END;
GO

CREATE PROCEDURE dbo.Clean
    AS
BEGIN
	DELETE FROM dbo.dnLog
	WHERE first_seen < DATEADD(HOUR, -48, GETDATE());
END
GO


CREATE PROCEDURE dbo.CalcStats
    AS
BEGIN
	UPDATE dbo.botlog
	SET install_calculated = first_seen
	WHERE install_calculated IS NULL
	  AND ABS(DATEDIFF(MINUTE, first_seen, last_seen)) <= 5;

	UPDATE dbo.botlog
	SET uninstall_calculated = last_seen
	WHERE uninstall_calculated IS NULL
	  AND DATEDIFF(DAY, first_seen, last_seen) > 10;
END
GO


DROP VIEW  if exists dbo.BotLogView;
GO

-- Create the view
CREATE VIEW dbo.BotLogView AS
SELECT TOP (1000) [id]
      ,[server]
      ,[first_seen]
      ,[last_seen]
      ,[first_seen_ip]
      ,[last_seen_ip]
      ,[serie]
      ,[number]
      ,[number_of_requests]
      ,[number_of_elevated_requests],

	  	( (Select count(*) from dnLog where dnLog.ip = 
	botLog.first_seen_ip and cast(dnLog.first_seen as date) = CAST(botlog.first_seen AS DATE)))
	as number_of_downloads

  FROM [hephaestus].[dbo].[botLog]
GO



DROP VIEW  if exists dbo.DownloadLogView;
GO

CREATE VIEW dbo.DownloadLogView AS
SELECT TOP (1000) [ip]
      ,[server]
      ,[profile]
      ,[first_seen]
      ,[last_seen]
      ,[number_of_requests]
  FROM [hephaestus].[dbo].[dnLog]
GO

  

IF OBJECT_ID('dbo.DailyServerSerieStatsView', 'V') IS NOT NULL
    DROP VIEW dbo.DailyServerSerieStatsView;
GO

-- Create the view
CREATE VIEW dbo.DailyServerSerieStatsView AS
SELECT
    CAST(first_seen AS DATE) AS Date,
    server,
    ISNULL(serie, 'not specified') AS Serie,
    COUNT(DISTINCT id) AS UniqueIDCount,
	COUNT(DISTINCT CASE WHEN number_of_elevated_requests > 0 THEN id END) AS ElevatedUniqueIDCount,

	( (Select count(*) from dnLog where dnLog.ip = 
	min(botLog.first_seen_ip) and cast(dnLog.first_seen as date) = CAST(botlog.first_seen AS DATE)))
	as NumberOfDownloads,

sum(CASE WHEN install_calculated is not null and CAST(first_seen AS DATE) = CAST(install_calculated AS DATE) THEN 1 else 0 END) AS InstallCount,

sum(CASE WHEN uninstall_calculated is not null and CAST(last_seen AS DATE) = CAST(uninstall_calculated AS DATE) THEN 1 else 0 END) AS UnInstallCount
FROM
    dbo.botLog
GROUP BY
    CAST(first_seen AS DATE),
    server,
    ISNULL(serie, 'not specified');
GO



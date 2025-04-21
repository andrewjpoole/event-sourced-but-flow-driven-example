/* RESET
DELETE FROM [WeatherAppDb].[dbo].[DomainEvents]
DELETE FROM [WeatherAppDb].[dbo].[OutboxItemStatus]
DELETE FROM [WeatherAppDb].[dbo].[OutboxItems]
*/

SELECT TOP (1000) [Id]
      ,[StreamId]
      ,[Version]
      ,[EventClassName]
      ,[SerialisedEvent]
      ,[TimestampCreatedUtc]
      ,[IdempotencyKey]
  FROM [WeatherAppDb].[dbo].[DomainEvents]

-- Get all outbox records with their latest statuses
;WITH LatestStatus AS
(
    SELECT
        [OutboxItemId],
        [Status],
        [NotBefore],
        ROW_NUMBER() OVER (PARTITION BY [OutboxItemId] ORDER BY [Created] DESC) AS RowNum
    FROM [WeatherAppDb].[dbo].[OutboxItemStatus]
)
SELECT
    OI.*,
    CASE 
        WHEN LS.[Status] = 1 THEN 'Sent'
        WHEN LS.[Status] = 2 THEN 'TransientFailure'
        WHEN LS.[Status] = 3 THEN 'Scheduled'
        WHEN LS.[Status] = 4 THEN 'Cancelled'
        WHEN LS.[Status] IS NULL THEN 'Pending'
    END AS [Status],
    LS.[NotBefore]
FROM [WeatherAppDb].[dbo].[OutboxItems] OI WITH (UPDLOCK, READPAST)
LEFT JOIN LatestStatus LS
    ON OI.[Id] = LS.[OutboxItemId] AND LS.RowNum = 1


-- Get all outbox records
SELECT TOP (1000) [Id]
      ,[TypeName]
      ,[SerialisedData]
      ,[MessagingEntityName]
      ,[Created]
  FROM [WeatherAppDb].[dbo].[OutboxItems]


-- Get all outbox sent status records
SELECT *,
    CASE 
        WHEN [Status] = 1 THEN 'Sent'
        WHEN [Status] = 2 THEN 'TransientFailure'
        WHEN [Status] = 3 THEN 'Scheduled'
        WHEN [Status] = 4 THEN 'Cancelled'
        WHEN [Status] IS NULL THEN 'Pending'
    END AS [Status]
FROM [WeatherAppDb].[dbo].[OutboxItemStatus]


BEGIN TRAN; -- transaction allows locking, we are not updating anything.
    WITH LatestStatus AS
    (
        SELECT
            [OutboxItemId],
            [Status],
            [NotBefore],
            ROW_NUMBER() OVER (PARTITION BY [OutboxItemId] ORDER BY [Created] DESC) AS RowNum
        FROM [WeatherAppDb].[dbo].[OutboxItemStatus]
    )
    SELECT TOP (3) -- Batch size of 3 
        OI.*,
        CASE 
            WHEN LS.[Status] = 1 THEN 'Sent'
            WHEN LS.[Status] = 2 THEN 'TransientFailure'
            WHEN LS.[Status] = 3 THEN 'Scheduled'
            WHEN LS.[Status] = 4 THEN 'Cancelled'
            WHEN LS.[Status] IS NULL THEN 'Pending'
        END AS [Status],
        LS.[NotBefore]
    FROM [WeatherAppDb].[dbo].[OutboxItems] OI WITH (UPDLOCK, READPAST)
    LEFT JOIN LatestStatus LS
        ON OI.[Id] = LS.[OutboxItemId] AND LS.RowNum = 1
    WHERE
        LS.[OutboxItemId] IS NULL -- No status record exists yet, OutboxItem is pending
        OR (LS.[Status] = 2 AND LS.[NotBefore] < GETUTCDATE()) -- Transient Failure and NotBefore has past        
        OR (LS.[Status] = 3 AND LS.[NotBefore] < GETUTCDATE()) -- Scheduled and NotBefore has past        
    ORDER BY OI.[Created];  
ROLLBACK
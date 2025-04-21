CREATE TABLE DomainEvents (
    [Id] BIGINT PRIMARY KEY IDENTITY(1,1), -- PK
    [StreamId] UNIQUEIDENTIFIER NOT NULL, -- Stream ID (GUID)
    [Version] INT NOT NULL, -- Version of the event
    [EventClassName] NVARCHAR(255) NOT NULL, -- Name of the event class
    [SerialisedEvent] NVARCHAR(MAX) NOT NULL, -- Serialized event data
    [TimestampCreatedUtc] DATETIME2 NOT NULL DEFAULT GETUTCDATE(), -- Timestamp of creation in UTC
    [IdempotencyKey] AS CAST(JSON_VALUE(SerialisedEvent, '$.IdempotencyKey') AS varchar(255)) -- Computed column for IdempotencyKey from JSON
);
GO

-- Index on StreamId to optimize queries filtering by StreamId
CREATE INDEX IX_DomainEvents_StreamId ON [DomainEvents] ([StreamId]);
GO

-- Unique Index on StreamId and Version
CREATE UNIQUE INDEX IX_DomainEvents_StreamId_Version ON [DomainEvents] ([StreamId], [Version]);
GO

-- To speed up queries for properties from within SerialisedEvent, we can create a computed columns and an index.
CREATE UNIQUE INDEX IX_DomainEvents_IdempotencyKey ON [DomainEvents] ([IdempotencyKey]) WHERE EventClassName = 'WeatherDataCollectionInitiated';
GO
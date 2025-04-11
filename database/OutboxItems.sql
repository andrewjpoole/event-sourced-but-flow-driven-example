CREATE TABLE [dbo].[OutboxItems]
(
  [Id] BIGINT PRIMARY KEY IDENTITY(1,1), -- PK
  [AssociatedId] VARCHAR(255) NOT NULL, -- The ID of the associated aggregate, e.g. the collected weather data streamID
  [TypeName] VARCHAR(255) NOT NULL, -- The full type name of the message, used for deserialization
  [SerialisedData] VARCHAR(MAX) NOT NULL, -- The serialized message as json string
  [MessagingEntityName] VARCHAR(255) NOT NULL, -- The name of the messaging entity, e.g. the topic or queue name
  [SerialisedTelemetry] VARCHAR(MAX) NOT NULL, -- The telemetry data as json string
  [Created] DATETIMEOFFSET NOT NULL -- When the outbox item was created
)

CREATE TABLE [dbo].[OutboxItemStatus]
(
  [Id] BIGINT PRIMARY KEY IDENTITY(1,1), -- PK
  [OutboxItemId] BIGINT NOT NULL FOREIGN KEY REFERENCES OutboxItems(Id), -- FK to OutboxItems
  [Status] TINYINT NOT NULL, -- Sent | TansientFailure | Scheduled | Cancelled
  [NotBefore] DATETIMEOFFSET NULL, -- Used with TransientFailure for exponential backoff, but not Cancelled
  [Created] DATETIMEOFFSET NOT NULL -- When the status was set
)


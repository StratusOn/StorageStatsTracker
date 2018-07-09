SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[BlobStorageEventData] (
    [BlobEventId]                     BIGINT           IDENTITY (1, 1) NOT NULL,
    [topic]                           NVARCHAR (100)   NOT NULL,
    [subject]                         NVARCHAR (2048)  NOT NULL,
    [eventType]                       NVARCHAR (50)    NOT NULL,
    [eventTime]                       NVARCHAR (50)    NOT NULL,
    [id]                              UNIQUEIDENTIFIER NOT NULL,
    [data.api]                        NCHAR (30)       NULL,
    [data.clientRequestId]            UNIQUEIDENTIFIER NULL,
    [data.requestId]                  UNIQUEIDENTIFIER NULL,
    [data.eTag]                       NCHAR (20)       NULL,
    [data.contentType]                NCHAR (30)       NULL,
    [data.contentLength]              INT              NULL,
    [data.blobType]                   NCHAR (20)       NULL,
    [data.url]                        NVARCHAR (2048)  NULL,
    [data.sequencer]                  NVARCHAR (50)    NULL,
    [data.storageDiagnostics.batchId] UNIQUEIDENTIFIER NULL,
    [dataVersion]                     NVARCHAR (50)    NOT NULL,
    [metadataVersion]                 NVARCHAR (50)    NULL,
    [storageAccountDeletedData]       NVARCHAR (MAX)  NULL,
    [DateCreated]                     DATETIME         NOT NULL DEFAULT GETUTCDATE()
);



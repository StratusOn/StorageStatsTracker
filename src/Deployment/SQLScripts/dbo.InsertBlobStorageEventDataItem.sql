SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[InsertBlobStorageEventDataItem]
    @topic NVARCHAR(100), 
    @subject NVARCHAR(2048), 
    @eventType NVARCHAR(50), 
    @eventTime NVARCHAR(50), 
    @id UNIQUEIDENTIFIER, 
    @data_api NCHAR(30), 
    @data_clientRequestId UNIQUEIDENTIFIER = NULL, 
    @data_requestId UNIQUEIDENTIFIER, 
    @data_eTag NCHAR(20) = NULL, 
    @data_contentType NCHAR(30), 
    @data_contentLength INT = NULL, 
    @data_blobType NCHAR(20), 
    @data_url NVARCHAR(2048), 
    @data_sequencer NVARCHAR(50), 
    @data_storageDiagnostics_batchId UNIQUEIDENTIFIER, 
    @dataVersion NVARCHAR(50), 
    @metadataVersion NVARCHAR(50),
	@storageAccountDeletedData NVARCHAR(MAX) = NULL
AS
	SET NOCOUNT ON

	INSERT INTO [dbo].[BlobStorageEventData]
	(
		[topic],
		[subject],
		[eventType],
		[eventTime],
		[id],
		[data.api],
		[data.clientRequestId],
		[data.requestId],
		[data.eTag],
		[data.contentType],
		[data.contentLength],
		[data.blobType],
		[data.url],
		[data.sequencer],
		[data.storageDiagnostics.batchId],
		[dataVersion],
		[metadataVersion],
		[storageAccountDeletedData]
	)
	VALUES 
	(
		@topic, 
		@subject, 
		@eventType, 
		@eventTime, 
		@id, 
		@data_api, 
		@data_clientRequestId, 
		@data_requestId, 
		@data_eTag, 
		@data_contentType, 
		@data_contentLength, 
		@data_blobType, 
		@data_url, 
		@data_sequencer, 
		@data_storageDiagnostics_batchId, 
		@dataVersion, 
		@metadataVersion,
		@storageAccountDeletedData
	)

RETURN 0

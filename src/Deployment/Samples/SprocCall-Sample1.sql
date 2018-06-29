USE [sqldb1]
GO

DECLARE	@return_value Int

EXEC	@return_value = [dbo].[InsertBlobStorageEventDataItem]
		@topic = N'/subscriptions/{subscription-id}/resourceGroups/Storage/providers/Microsoft.Storage/storageAccounts/xstoretestaccount',
		@subject = N'/blobServices/default/containers/testcontainer/blobs/testfile.txt',
		@eventType = N'Microsoft.Storage.BlobDeleted',
		@eventTime = N'2017-11-07T20:09:22.5674003Z',
		@id = '4c2359fe-001e-00ba-0e04-58586806d298',
		@data_api = N'DeleteBlob',
		@data_requestId = '4c2359fe-001e-00ba-0e04-585868000000',
		@data_contentType = N'text/plain',
		@data_blobType = N'BlockBlob',
		@data_url = N'https://example.blob.core.windows.net/testcontainer/testfile.txt',
		@data_sequencer = N'0000000000000281000000000002F5CA',
		@data_storageDiagnostics_batchId = 'b68529f3-68cd-4744-baa4-3c0498ec19f0',
		@dataVersion = N'',
		@metadataVersion = N'1'

SELECT	@return_value as 'Return Value'

GO

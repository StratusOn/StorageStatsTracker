# Storage Stats Tracker
A simple solution that allows tracking new storage account creation as well as creation or deletion of blobs in a blob storage account. Events are stored in an Azure SQL Database.

## How this Works
* The solution sets up an event grid subscription on the creation/deletion of new blob storage accounts.
* Upon the creation of a storage account, an Azure Function is triggered. The triggered function creates a new event grid subscription on the creation/deletion of blobs in the newly created storage account.
* Upon the creation or deletion of a blob in the newly created storage account, a second Azure Function is triggered. The triggered function writes a record to the SQL database.
* Upon the deletion of a storage account, the same Azure Function for storage account creation is triggered. This trigger removes the event grid subscription that was created when the storage account was created. A record containing the event details is written to the SQL database by calling the second Azure Function.
* **NOTE**: Unfortunately, deleting a blob container does not cause events to be fired for the container or for any of the blobs it contains. This is a limitation of Event Grid blob storage subscriptions.

## The Storage Stats
* The stats can be read from the stats database table by running SQL queries against it.

## Deploying the Template
To deploy the template, click on the following button:

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FStratusOn%2FStorageStatsTracker%2Fmaster%2Fsrc%2FDeployment%2Fazuredeploy.json)

**NOTE**: If the SQL database is deployed separately, make sure to run the _dbo.BlobStorageEventData.sql_ and then _dbo.InsertBlobStorageEventDataItem.sql_ scripts found in the [SQLScripts](https://github.com/StratusOn/StorageStatsTracker/tree/master/src/Deployment/SQLScripts) folder. They are also written to the blobStorageEventFunction function next to the run.csx file.

## Acknowledgements
Based on his idea and design, this implementation would not be possible without the technical support from Tobias from Microsoft CSE (Commercial Software Engineering).

# Storage Stats Tracker
A simple solution that allows tracking new storage account creation as well as creation or deletion of blobs in a blob storage account. Events are stored in an Azure SQL Database.

## How this Works
* The solution sets up an event grid subscription on the creation/deletion of new blob storage accounts.
* Upon the creation of a storage account, an Azure Function is triggered. The triggered function creates a new event grid subscription on the creation/deletion of blobs in the newly created storage account. A record containing the event details is written to the SQL database.
* Upon the deletion of a storage account, the same Azure Function for storage account creation is triggered. This trigger removes the event grid subscription that was created when the storage account was created. A record containing the event details is written to the SQL database.
* Upon the creation or deletion of a blob in the newly created storage account, a second Azure Function is triggered. The triggered function writes a record to the SQL database.

## The Storage Stats
* The stats can be read from the database stats table by running SQL queries against it.

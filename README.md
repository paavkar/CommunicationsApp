# CommunicationsApp

## Introduction

The goal of this application was to provide similar-ish communications system to Discord with servers and channels.
Real-time communication is made possible with SignalR.

Technologies used:
- ASP.NET Core Identity
- Azure Cosmos DB for NoSQL,
- Blazor,
- Fluent UI,
- SignalR,
- SQL Server.

## Running locally

NOTE! The app targets .NET 10, so currently you need the [preview](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) version of it.

This software uses ASP.NET Core Identity to provide user authentication, so you need an SQL Server database for it.
Individual messages are in turn stored in an Azure Cosmos DB for NoSQL, so you should have at least the [emulator](https://learn.microsoft.com/en-us/azure/cosmos-db/how-to-develop-emulator?tabs=windows%2Ccsharp&pivots=api-nosql) available.

The application expects the following variables to be set up:
```
{
  "ConnectionStrings": {
    "DefaultConnection": "<SQL_DB_CONNECTION_STRING>"
  },
  "COSMOSDB_DATABASE": "<COSMOS_DB_NAME>",
  "COSMOSDB_ENDPOINT": "<COSMOS_DB_ENDPOINT>",
  "COSMOSDB_KEY": "<COSMOS_DB_KEY>",
  "COSMOS_DB_CONNECTION_STRING": "<COSMOS_DB_CONNECTION_STRING>"
}
```
If you're using the Cosmos DB Emulator, you're able to use its default values.  
The values being 

`localhost:8081` for endpoint, 

`C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==` for key, and

`AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;` for connection string.

For the SQL Server database, you need to run the migration(s) first. You can do this with the `Update-Database`
command if you're using the VS Package Manager Console, or `dotnet ef database update` if you're using the CLI.
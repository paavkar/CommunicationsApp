using Microsoft.Azure.Cosmos;

namespace CommunicationsApp.CosmosDb
{
    public class CosmosDbFactory
    {
        public CosmosClient CosmosClient { get; set; }
        public string DatabaseName { get; set; }

        public CosmosDbFactory(IConfiguration configuration)
        {
            DatabaseName = configuration["COSMOSDB_DATABASE"] ?? "communications-app";
            var account = configuration["COSMOSDB_ENDPOINT"]
                             ?? "https://localhost:8081";
            var key = configuration["COSMOSDB_KEY"]
                         ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            var connectionString = configuration["COSMOS_DB_CONNECTION_STRING"];
            var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";

            var serializationOptions = new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            };

            CosmosClient = environment.Equals("Development")
                ? new(account, key, new CosmosClientOptions()
                {
                    SerializerOptions = serializationOptions,
                    HttpClientFactory = () => new HttpClient(new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    }),
                })
                : new(connectionString, new CosmosClientOptions()
                {
                    SerializerOptions = serializationOptions
                });
        }

        public async Task InitializeDatabase()
        {
            await CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);

            var database = CosmosClient.GetDatabase(DatabaseName);

            await database.CreateContainerIfNotExistsAsync(id: "messages", partitionKeyPath: "/partitionKey");
        }
    }
}

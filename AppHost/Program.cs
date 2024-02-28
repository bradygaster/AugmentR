
var builder = DistributedApplication.CreateBuilder(args);

var pubsub  = // a redis container the app will use for simple messaging to the frontend
    builder.AddRedis("pubsub");

var qdrant = // the qdrant container the app will use for vector search
    builder.AddContainer("qdrant", "qdrant/qdrant")
           .WithEndpoint(hostPort: 6333, name: "qdrant", scheme: "http");

var histdb  = // a postgres container the app will use for history storage
    builder.AddPostgres("postgres")
           .AddDatabase("historydb");

var histsvc = // a minimal api app that will provide get/post access to the history database
    builder.AddProject<Projects.HistoryService>("historyservice")
           .WithReference(histdb);

_ =           // a .net app that will initialize the history database
    builder.AddProject<Projects.HistoryDb>("historydbapp")
           .WithReference(histsvc)
           .WithReference(histdb);

var storage = // an azure storage account
    builder.AddAzureStorage("storage")
               .UseEmulator(); // use azurite for local development

var blobs =   // a blob container in the storage account
    storage.AddBlobs("AzureBlobs");

var queues =  // a queue in the storage account
    storage.AddQueues("AzureQueues");

var backend = // the main .net app that will perform augmentation and vector search
    builder.AddProject<Projects.Backend>("backend")
           .WithEnvironment("QDRANT_ENDPOINT", qdrant.GetEndpoint("qdrant"))
           .WithReference(histsvc).WithReference(pubsub).WithReference(blobs).WithReference(queues);

_ =           // a blazor server app that will provide a web ui for the app
    builder.AddProject<Projects.Frontend>("frontend")
           .WithReference(histsvc).WithReference(backend).WithReference(pubsub).WithReference(queues);

builder.Build().Run();






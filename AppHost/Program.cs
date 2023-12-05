
var builder = DistributedApplication.CreateBuilder(args);

var pubsub  = // a redis container the app will use for simple messaging to the frontend
    builder.AddRedisContainer("pubsub");

var qdrant  = // the qdrant container the app will use for vector search
    builder.AddContainer("qdrant", "qdrant/qdrant")
           .WithServiceBinding(containerPort: 6333, name: "qdrant", scheme: "http");

var histdb  = // a postgres container the app will use for history storage
    builder.AddPostgresContainer("postgres")
           .AddDatabase("historydb");

var histsvc = // a minimal api app that will provide get/post access to the history database
    builder.AddProject<Projects.HistoryService>("historyservice")
           .WithReference(histdb);

_ =           // a .net app that will initialize the history database
    builder.AddProject<Projects.HistoryDb>("historydbapp")
           .WithReference(histsvc)
           .WithReference(histdb);

var backend = // the main .net app that will perform augmentation and vector search
    builder.AddProject<Projects.Backend>("backend")
           .WithEnvironment("QDRANT_ENDPOINT", qdrant.GetEndpoint("qdrant"))
           .WithReference(histsvc)
           .WithReference(pubsub);

_ =           // a blazor server app that will provide a web ui for the app
    builder.AddProject<Projects.Frontend>("frontend")
           .WithReference(histsvc)
           .WithReference(backend)
           .WithReference(pubsub);

builder.Build().Run();






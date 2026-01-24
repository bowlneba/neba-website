var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-userName", "neba-db-user");

var postgres = builder.AddPostgres("postgres")
    .WithUserName(postgresUser)
    .WithPgAdmin()
    .WithDataVolume("neba-website-data");

var database = postgres.AddDatabase("neba-website");

var apiService = builder.AddProject<Projects.Neba_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(apiService)
    .WaitFor(apiService);

await builder.Build().RunAsync();
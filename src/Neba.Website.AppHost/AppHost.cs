var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Neba_Website_Api>("api")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Neba_Website_Server>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

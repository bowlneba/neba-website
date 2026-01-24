var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Neba_Website_Api>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Neba_Website_Server>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

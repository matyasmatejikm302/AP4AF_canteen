using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

builder.AddProject<UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

var webapi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
                    .WithReference(database)
                    .WaitFor(database);

builder.AddProject<Projects.UTB_Minute_Web>("web")
       .WithReference(webapi)
       .WaitFor(webapi);

builder.Build().Run();
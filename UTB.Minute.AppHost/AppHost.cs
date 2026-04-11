var builder = DistributedApplication.CreateBuilder(args);

// Vytvoření PostgreSQL databáze
var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

// DbManager s tlačítkem pro reset/seed
builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

// Hlavní WebApi
builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
       .WithReference(database)
       .WaitFor(database);

builder.Build().Run();
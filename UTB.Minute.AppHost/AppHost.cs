using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Databáze
var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

// 2. --- NOVINKA: Spuštění Keycloaku ---
var keycloak = builder.AddKeycloak("keycloak")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

// 3. DbManager
builder.AddProject<UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

// 4. WebApi - předáme referenci na Keycloak
var webapi = builder.AddProject<UTB_Minute_WebApi>("webapi")
                    .WithReference(database)
                    .WithReference(keycloak) // <--- PŘIDÁNO
                    .WaitFor(database);

// 5. Web Frontend - předáme referenci na Keycloak
builder.AddProject<UTB_Minute_Web>("web")
       .WithReference(webapi)
       .WithReference(keycloak) // <--- PŘIDÁNO
       .WaitFor(webapi);

builder.Build().Run();
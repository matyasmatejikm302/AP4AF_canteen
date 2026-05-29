using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Databáze
var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

// 2. Keycloak - Správa uživatelů
// Vytvoříme instanci Keycloaku na portu 8080
var keycloak = builder.AddKeycloak("keycloak")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

// 3. DbManager
builder.AddProject<UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

// 4. WebApi - Musí vědět o databázi i o Keycloaku (pro ověřování tokenů)
var webapi = builder.AddProject<UTB_Minute_WebApi>("webapi")
                    .WithReference(database)
                    .WithReference(keycloak)
                    .WaitFor(database);

// 5. Web Frontend - Musí vědět o WebApi a Keycloaku (pro přihlášení)
builder.AddProject<UTB_Minute_Web>("web")
       .WithReference(webapi)
       .WithReference(keycloak)
       .WaitFor(webapi);

builder.Build().Run();
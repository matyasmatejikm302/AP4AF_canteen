using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Databáze
var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

// 2. Spuštění Keycloaku
var keycloak = builder.AddKeycloak("keycloak")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

// 3. DbManager
builder.AddProject<UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

// 4. WebApi - předáme referenci na Keycloak a počkáme, až bude zdravý
var webapi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
                    .WithReference(database)
                    .WithReference(keycloak)
                    .WaitFor(database)
                    .WaitFor(keycloak); // <--- ČEKÁME NA KEYCLOAK

// 5. Web Frontend - předáme referenci na WebApi, Keycloak a počkáme na oba
builder.AddProject<Projects.UTB_Minute_Web>("web")
       .WithReference(webapi)
       .WithReference(keycloak)
       .WaitFor(webapi)
       .WaitFor(keycloak); // <--- ČEKÁME NA KEYCLOAK

builder.Build().Run();
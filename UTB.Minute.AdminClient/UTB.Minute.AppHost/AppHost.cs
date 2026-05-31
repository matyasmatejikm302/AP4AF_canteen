using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Databáze
var postgres = builder.AddPostgres("postgres")
                      .WithContainerName("postgres-UTB.Minute")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("CanteenDb");

// 2. Spuštění Keycloaku s automatickým importem
var keycloak = builder.AddKeycloak("keycloak")
                      .WithRealmImport("realm.json")
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);

// 3. DbManager
builder.AddProject<UTB_Minute_DbManager>("dbmanager")
       .WithReference(database)
       .WithHttpCommand("/dev/seed", "Restart Database")
       .WaitFor(database);

// 4. WebApi
var webapi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
                    .WithReference(database)
                    .WithReference(keycloak)
                    .WaitFor(database)
                    .WaitFor(keycloak);

// 5. Web Frontend
builder.AddProject<Projects.UTB_Minute_Web>("web")
       .WithReference(webapi)
       .WithReference(keycloak)
       .WaitFor(webapi)
       .WaitFor(keycloak);

builder.Build().Run();

// Definice jmenného prostoru pro obchvat source generátoru v testech
namespace UTB.Minute.AppHost
{
    public partial class Program { }
}
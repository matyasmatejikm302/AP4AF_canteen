var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.UTB_Minute_WebApi>("utb-minute-webapi");

builder.AddProject<Projects.UTB_Minute_DbManager>("utb-minute-dbmanager");

builder.Build().Run();

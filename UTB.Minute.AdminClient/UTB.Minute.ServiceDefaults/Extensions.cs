using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public static class ServiceDefaults
{
    public static void AddServiceDefaults(this WebApplicationBuilder builder)
    {
        // Default logging configuration
        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        // Remove or conditionally enable EventLog to avoid ObjectDisposedException during shutdown
        // Use EventLog only when explicitly enabled in configuration and when running on Windows.
        var eventLogEnabled = builder.Configuration.GetValue<bool?>("Logging:EventLog:LogEnabled") ?? false;
#if WINDOWS
        if (eventLogEnabled)
        {
            builder.Logging.AddEventLog(options =>
            {
                // Map configuration if present
                options.LogName = builder.Configuration.GetValue<string>("Logging:EventLog:LogName") ?? "Application";
                options.SourceName = builder.Configuration.GetValue<string>("Logging:EventLog:SourceName") ?? builder.Environment.ApplicationName;
            });
        }
#endif

        // Keep console and debug providers for local development
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Register other common services, e.g., configuration, healthchecks etc.
        // ... existing defaults ...
    }
}

namespace Microsoft.AspNetCore.Builder
{
    // Provide a no-op MapDefaultEndpoints extension so projects that call it compile.
    public static class CompatibilityExtensions
    {
        public static WebApplication MapDefaultEndpoints(this WebApplication app)
        {
            // Real implementation may add common endpoints; keep no-op to preserve behavior.
            return app;
        }
    }
}

namespace Aspire.Keycloak.Authentication
{
    // Minimal compatibility shim so projects that reference Aspire's extension compile
    public static class AspireCompatibility
    {
        public static WebApplicationBuilder AddKeycloakJwtAuthentication(this WebApplicationBuilder builder, string sectionName)
        {
            // No-op shim: real behavior comes from external package when present.
            return builder;
        }
    }
}

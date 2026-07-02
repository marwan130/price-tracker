namespace PriceTracker.API.Extensions;

public static class ConfigurationExtensions
{
    private const int MinJwtSecretLength    = 32;
    private const int MinSharedSecretLength = 16;

    public static WebApplicationBuilder MapPlatformConfiguration(this WebApplicationBuilder builder)
    {
        var overrides = new Dictionary<string, string?>();

        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Default"))
            && !string.IsNullOrWhiteSpace(builder.Configuration["DATABASE_URL"]))
        {
            overrides["ConnectionStrings:Default"] =
                NormalizePostgresUrl(builder.Configuration["DATABASE_URL"]!);
        }

        var allowedHosts = builder.Configuration["AllowedHosts"];
        var flyAppName   = builder.Configuration["FLY_APP_NAME"];

        if ((string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
            && !string.IsNullOrWhiteSpace(flyAppName))
        {
            overrides["AllowedHosts"] = $"{flyAppName}.fly.dev";
        }

        if (overrides.Count > 0)
            builder.Configuration.AddInMemoryCollection(overrides);

        return builder;
    }

    public static WebApplicationBuilder ValidateProductionSettings(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            return builder;

        var config  = builder.Configuration;
        var missing = new List<string>();
        var invalid = new List<string>();

        if (string.IsNullOrWhiteSpace(config.GetConnectionString("Default")))
            missing.Add("ConnectionStrings:Default");

        ValidateSecret(config["Jwt:Secret"], "Jwt:Secret", MinJwtSecretLength, missing, invalid);
        ValidateSecret(config["InternalApi:Key"], "InternalApi:Key", MinSharedSecretLength, missing, invalid);
        ValidateSecret(config["Hangfire:DashboardApiKey"], "Hangfire:DashboardApiKey", MinSharedSecretLength, missing, invalid);

        if (string.IsNullOrWhiteSpace(config["Smtp:Host"]))
            missing.Add("Smtp:Host");
        if (string.IsNullOrWhiteSpace(config["Smtp:Username"]))
            missing.Add("Smtp:Username");
        if (string.IsNullOrWhiteSpace(config["Smtp:Password"]))
            missing.Add("Smtp:Password");
        if (string.IsNullOrWhiteSpace(config["Smtp:From"]))
            missing.Add("Smtp:From");

        if (string.IsNullOrWhiteSpace(config["Frontend:BaseUrl"])
            && string.IsNullOrWhiteSpace(config["PUBLIC_APP_URL"]))
        {
            missing.Add("Frontend:BaseUrl or PUBLIC_APP_URL");
        }

        var allowedHosts = config["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
            missing.Add("AllowedHosts (must be specific hostnames, not '*')");

        var origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0 || origins.All(string.IsNullOrWhiteSpace))
            missing.Add("Cors:AllowedOrigins (at least one frontend origin required)");

        if (missing.Count > 0 || invalid.Count > 0)
        {
            var problems = missing.Concat(invalid);
            throw new InvalidOperationException(
                $"Invalid configuration for environment '{builder.Environment.EnvironmentName}': " +
                string.Join(", ", problems) +
                ". Set values via environment variables, Fly.io secrets, or appsettings.Production.json (not committed).");
        }

        return builder;
    }

    private static void ValidateSecret(
        string? value,
        string  key,
        int     minLength,
        List<string> missing,
        List<string> invalid)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add(key);
            return;
        }

        if (value.Length < minLength)
            invalid.Add($"{key} (must be at least {minLength} characters)");
    }

    /// <summary>
    /// Fly Postgres sets DATABASE_URL as postgres://…; Npgsql expects postgresql://….
    /// </summary>
    private static string NormalizePostgresUrl(string url)
    {
        if (url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return "postgresql://" + url["postgres://".Length..];

        return url;
    }
}

namespace PriceTracker.API.Extensions;

public static class ConfigurationExtensions
{
    public static WebApplicationBuilder ValidateProductionSettings(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            return builder;

        var config  = builder.Configuration;
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(config.GetConnectionString("Default")))
            missing.Add("ConnectionStrings:Default");

        if (string.IsNullOrWhiteSpace(config["Jwt:Secret"]))
            missing.Add("Jwt:Secret");

        if (string.IsNullOrWhiteSpace(config["InternalApi:Key"]))
            missing.Add("InternalApi:Key");

        if (string.IsNullOrWhiteSpace(config["Hangfire:DashboardApiKey"]))
            missing.Add("Hangfire:DashboardApiKey");

        if (string.IsNullOrWhiteSpace(config["Smtp:Host"]))
            missing.Add("Smtp:Host");
        if (string.IsNullOrWhiteSpace(config["Smtp:Username"]))
            missing.Add("Smtp:Username");
        if (string.IsNullOrWhiteSpace(config["Smtp:Password"]))
            missing.Add("Smtp:Password");
        if (string.IsNullOrWhiteSpace(config["Smtp:From"]))
            missing.Add("Smtp:From");

        if (string.IsNullOrWhiteSpace(config["Frontend:BaseUrl"])
            && string.IsNullOrWhiteSpace(config["PUBLIC_APP_URL"])
            && string.IsNullOrWhiteSpace(config["RAILWAY_PUBLIC_DOMAIN"]))
        {
            missing.Add("Frontend:BaseUrl or PUBLIC_APP_URL");
        }

        var allowedHosts = config["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
            missing.Add("AllowedHosts (must be specific hostnames, not '*')");

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required configuration is missing for environment '{builder.Environment.EnvironmentName}': " +
                string.Join(", ", missing) +
                ". Set values via environment variables or appsettings.Production.json (not committed).");
        }

        return builder;
    }
}

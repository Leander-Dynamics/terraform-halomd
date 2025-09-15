        app.Run();
    }

    private static void ConfigureCorsPolicy(CorsPolicyBuilder policyBuilder, string[] allowedOrigins, string[] allowedMethods, string[] allowedHeaders)
    {
        if (allowedOrigins.Length == 0 || allowedOrigins.Any(origin => string.Equals(origin, "*", StringComparison.Ordinal)))
        {
            policyBuilder.AllowAnyOrigin();
        }
        else
        {
            policyBuilder.WithOrigins(allowedOrigins);
        }

        if (allowedMethods.Length == 0 || allowedMethods.Any(method => string.Equals(method, "*", StringComparison.Ordinal)))
        {
            policyBuilder.AllowAnyMethod();
        }
        else
        {
            policyBuilder.WithMethods(allowedMethods);
        }

        if (allowedHeaders.Length == 0 || allowedHeaders.Any(header => string.Equals(header, "*", StringComparison.Ordinal)))
        {
            policyBuilder.AllowAnyHeader();
        }
        else
        {
            policyBuilder.WithHeaders(allowedHeaders);
        }
    }

    private static string[] GetConfiguredValues(IConfiguration configuration, string key)
    {
        var section = configuration.GetSection(key);
        var values = section.Get<string[]>() ?? Array.Empty<string>();

        if (values.Length > 0)
        {
            values = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToArray();
        }

        if (values.Length == 0 && !string.IsNullOrWhiteSpace(section.Value))
        {
            values = section.Value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim())
                .ToArray();
        }

        return values;
    }
}

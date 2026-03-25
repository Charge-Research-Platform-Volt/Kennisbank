using System.Collections.Concurrent;

namespace KnowledgeBank.Utils;

// Enumeration for environment variable names used in the application.
public enum EnvironmentVariable
{

    // ASP.NET
    ASPNETCORE_ENVIRONMENT,

    // PostgreSQL
    DATABASE_CONNECTION_STRING,

    // S3 Storage
    S3_ENDPOINT,
    S3_ACCESS_KEY,
    S3_SECRET_KEY,
    S3_USE_SSL,
    S3_REGION,
    S3_BUCKET_NAME,

    // Client URL
    HOST_URL,

    // Embeddings
    EMBEDDINGS_MODEL_NAME,
    EMBEDDINGS_ENDPOINT,
    EMBEDDINGS_API_KEY,

    // Chat Completions
    MISTRAL_ENDPOINT,
    MISTRAL_API_KEY,
    CHAT_MODEL_NAME,

    // Headscale
    HEADSCALE_URL,
    HEADSCALE_API_KEY
}

public class EnvironmentConfig
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<EnvironmentVariable, string> _variableValues = new();
    private readonly Dictionary<EnvironmentVariable, string> _variableNames = new();

    /// <summary>
    /// Initializes a new instance of the EnvironmentConfig class with the specified configuration.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance used to access application configuration settings.</param>
    public EnvironmentConfig(IConfiguration configuration)
    {
        _configuration = configuration;

        // Initialize environment variable names
        // ASP.NET
        _variableNames.Add(EnvironmentVariable.ASPNETCORE_ENVIRONMENT, "ASPNETCORE_ENVIRONMENT");

        // PostgreSQL
        _variableNames.Add(EnvironmentVariable.DATABASE_CONNECTION_STRING, "DATABASE_CONNECTION_STRING");

        // S3 Storage
        _variableNames.Add(EnvironmentVariable.S3_ENDPOINT, "S3_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.S3_ACCESS_KEY, "S3_ACCESS_KEY");
        _variableNames.Add(EnvironmentVariable.S3_SECRET_KEY, "S3_SECRET_KEY");
        _variableNames.Add(EnvironmentVariable.S3_USE_SSL, "S3_USE_SSL");
        _variableNames.Add(EnvironmentVariable.S3_REGION, "S3_REGION");
        _variableNames.Add(EnvironmentVariable.S3_BUCKET_NAME, "S3_BUCKET_NAME");

        // Client URL
        _variableNames.Add(EnvironmentVariable.HOST_URL, "HOST_URL");

        // Embeddings
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_MODEL_NAME, "EMBEDDINGS_MODEL_NAME");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_ENDPOINT, "EMBEDDINGS_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_API_KEY, "EMBEDDINGS_API_KEY");

        // Chat Completions
        _variableNames.Add(EnvironmentVariable.MISTRAL_ENDPOINT, "MISTRAL_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.MISTRAL_API_KEY, "MISTRAL_API_KEY");
        _variableNames.Add(EnvironmentVariable.CHAT_MODEL_NAME, "CHAT_MODEL_NAME");

        // Headscale
        _variableNames.Add(EnvironmentVariable.HEADSCALE_URL, "HEADSCALE_URL");
        _variableNames.Add(EnvironmentVariable.HEADSCALE_API_KEY, "HEADSCALE_API_KEY");
    }

    /// <summary>
    /// Gets the value of the specified environment variable.
    /// </summary>
    /// <param name="variable">The environment variable to retrieve the value for.</param>
    /// <returns>The value of the environment variable if it exists and is not empty.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the environment variable is not defined or when it is defined but has no value.
    /// </exception>
    public string GetVariableValue(EnvironmentVariable variable)
    {
        if (_variableValues.TryGetValue(variable, out string? value))
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            throw new ArgumentException($"Environment variable {variable} is defined but has no value.");
        }

        throw new ArgumentException($"Tried to access environment variable {variable}, but it is not defined. Check the .env file and EnvironmentConfig.cs.");
    }

    /// <summary>
    /// Validates that all required environment variables are present and have non-empty values.
    /// Populates the internal variable values dictionary with the retrieved configuration values.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when any required environment variable is not defined or has an empty value.
    /// The exception message includes the name of the missing variable and guidance to check the .env file and EnvironmentConfig.cs.
    /// </exception>
    public void CheckEnvironmentVariables()
    {
        foreach (var variable in _variableNames)
        {
            string? value = _configuration.GetValue<string>(variable.Value);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Environment variable {variable.Value} is not defined. Check the .env file and EnvironmentConfig.cs.");
            }

            _variableValues[variable.Key] = value;
        }
    }
}

using System.Collections.Concurrent;
using DotNetEnv;

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
    EMBEDDINGS_REQUESTS_PER_MINUTE,
    EMBEDDINGS_TOKENS_PER_MINUTE,
    EMBEDDINGS_MAX_CONCURRENT_REQUESTS,

    // Chat Completions
    MISTRAL_ENDPOINT,
    MISTRAL_API_KEY,
    MISTRAL_SMALL_MODEL_NAME,
    MISTRAL_SMALL_REQUESTS_PER_MINUTE,
    MISTRAL_SMALL_TOKENS_PER_MINUTE,
    MISTRAL_MEDIUM_MODEL_NAME,
    MISTRAL_MEDIUM_REQUESTS_PER_MINUTE,
    MISTRAL_MEDIUM_TOKENS_PER_MINUTE,
    MISTRAL_OCR_MODEL_NAME,
    MISTRAL_OCR_PAGES_PER_MINUTE,

    // Headscale
    HEADSCALE_URL,
    HEADSCALE_API_KEY,

    // Meilisearch
    MEILISEARCH_URL,
    MEILISEARCH_API_KEY,

    // Webscrape service
    WEBSCRAPE_SERVICE_URL,
}

public class EnvironmentConfig
{
    private readonly IConfiguration configuration;
    private readonly ConcurrentDictionary<EnvironmentVariable, string> variableValues = new();
    private readonly Dictionary<EnvironmentVariable, string> variableNames = [];

    /// <summary>
    /// Initializes a new instance of the EnvironmentConfig class with the specified configuration.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance used to access application configuration settings.</param>
    public EnvironmentConfig(IConfiguration configuration)
    {
        this.configuration = configuration;

        // Initialize environment variable names
        // ASP.NET
        variableNames.Add(EnvironmentVariable.ASPNETCORE_ENVIRONMENT, "ASPNETCORE_ENVIRONMENT");

        // PostgreSQL
        variableNames.Add(EnvironmentVariable.DATABASE_CONNECTION_STRING, "DATABASE_CONNECTION_STRING");

        // S3 Storage
        variableNames.Add(EnvironmentVariable.S3_ENDPOINT, "S3_ENDPOINT");
        variableNames.Add(EnvironmentVariable.S3_ACCESS_KEY, "S3_ACCESS_KEY");
        variableNames.Add(EnvironmentVariable.S3_SECRET_KEY, "S3_SECRET_KEY");
        variableNames.Add(EnvironmentVariable.S3_USE_SSL, "S3_USE_SSL");
        variableNames.Add(EnvironmentVariable.S3_REGION, "S3_REGION");
        variableNames.Add(EnvironmentVariable.S3_BUCKET_NAME, "S3_BUCKET_NAME");

        // Client URL
        variableNames.Add(EnvironmentVariable.HOST_URL, "HOST_URL");

        // Embeddings
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_MODEL_NAME, "EMBEDDINGS_MODEL_NAME");
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_ENDPOINT, "EMBEDDINGS_ENDPOINT");
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_API_KEY, "EMBEDDINGS_API_KEY");
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_REQUESTS_PER_MINUTE, "EMBEDDINGS_REQUESTS_PER_MINUTE");
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_TOKENS_PER_MINUTE, "EMBEDDINGS_TOKENS_PER_MINUTE");
        variableNames.Add(EnvironmentVariable.EMBEDDINGS_MAX_CONCURRENT_REQUESTS, "EMBEDDINGS_MAX_CONCURRENT_REQUESTS");

        // Chat Completions
        variableNames.Add(EnvironmentVariable.MISTRAL_ENDPOINT, "MISTRAL_ENDPOINT");
        variableNames.Add(EnvironmentVariable.MISTRAL_API_KEY, "MISTRAL_API_KEY");
        variableNames.Add(EnvironmentVariable.MISTRAL_SMALL_MODEL_NAME, "MISTRAL_SMALL_MODEL_NAME");
        variableNames.Add(EnvironmentVariable.MISTRAL_SMALL_REQUESTS_PER_MINUTE, "MISTRAL_SMALL_REQUESTS_PER_MINUTE");
        variableNames.Add(EnvironmentVariable.MISTRAL_SMALL_TOKENS_PER_MINUTE, "MISTRAL_SMALL_TOKENS_PER_MINUTE");

        variableNames.Add(EnvironmentVariable.MISTRAL_MEDIUM_MODEL_NAME, "MISTRAL_MEDIUM_MODEL_NAME");
        variableNames.Add(EnvironmentVariable.MISTRAL_MEDIUM_REQUESTS_PER_MINUTE, "MISTRAL_MEDIUM_REQUESTS_PER_MINUTE");
        variableNames.Add(EnvironmentVariable.MISTRAL_MEDIUM_TOKENS_PER_MINUTE, "MISTRAL_MEDIUM_TOKENS_PER_MINUTE");

        variableNames.Add(EnvironmentVariable.MISTRAL_OCR_MODEL_NAME, "MISTRAL_OCR_MODEL_NAME");
        variableNames.Add(EnvironmentVariable.MISTRAL_OCR_PAGES_PER_MINUTE, "MISTRAL_OCR_PAGES_PER_MINUTE");

        // Headscale
        variableNames.Add(EnvironmentVariable.HEADSCALE_URL, "HEADSCALE_URL");
        variableNames.Add(EnvironmentVariable.HEADSCALE_API_KEY, "HEADSCALE_API_KEY");

        // Meilisearch
        variableNames.Add(EnvironmentVariable.MEILISEARCH_URL, "MEILISEARCH_URL");
        variableNames.Add(EnvironmentVariable.MEILISEARCH_API_KEY, "MEILISEARCH_API_KEY");

        // Webscrape service
        variableNames.Add(EnvironmentVariable.WEBSCRAPE_SERVICE_URL, "WEBSCRAPE_SERVICE_URL");
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
        if (variableValues.TryGetValue(variable, out string? value))
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
        foreach (var variable in variableNames)
        {
            string? value = configuration.GetValue<string>(variable.Value);
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Environment variable {variable.Value} is not defined. Check the .env file and EnvironmentConfig.cs.");
            }

            variableValues[variable.Key] = value;
        }
    }
}

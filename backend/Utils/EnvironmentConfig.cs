using System.Collections.Concurrent;

namespace KnowledgeBank.Utils;

// Enumeration for environment variable names used in the application.
public enum EnvironmentVariable
{

    // ASP.NET
    ASPNETCORE_ENVIRONMENT,

    // PostgreSQL
    DATABASE_CONNECTION_STRING,

    // Azure Storage
    STORAGE_CONNECTION_STRING,

    // Client URL
    HOST_URL,


    // Vector DB
    QDRANT_EMBEDDINGS_DIMENSIONS,
    QDRANT_COLLECTION_NAME,
    QDRANT_HOST,
    QDRANT_API_KEY,
    QDRANT_HTTPS,

    // Embeddings
    EMBEDDINGS_MODEL_NAME,
    EMBEDDINGS_CLIENT_ENDPOINT,
    EMBEDDINGS_CLIENT_API_KEY,

    // Chat Completions
    CHAT_DEPLOYMENT_NAME,
    AZURE_OPENAI_CLIENT_ENDPOINT,
    AZURE_OPENAI_CLIENT_API_KEY,


    // Document Intelligence
    DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT,
    DOCUMENT_INTELLIGENCE_CLIENT_API_KEY,
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

        // Azure Storage
        _variableNames.Add(EnvironmentVariable.STORAGE_CONNECTION_STRING, "STORAGE_CONNECTION_STRING");

        // Client URL
        _variableNames.Add(EnvironmentVariable.HOST_URL, "HOST_URL");

        // Vector DB
        _variableNames.Add(EnvironmentVariable.QDRANT_EMBEDDINGS_DIMENSIONS, "QDRANT_EMBEDDINGS_DIMENSIONS");
        _variableNames.Add(EnvironmentVariable.QDRANT_COLLECTION_NAME, "QDRANT_COLLECTION_NAME");
        _variableNames.Add(EnvironmentVariable.QDRANT_HOST, "QDRANT_HOST");
        _variableNames.Add(EnvironmentVariable.QDRANT_API_KEY, "QDRANT_API_KEY");
        _variableNames.Add(EnvironmentVariable.QDRANT_HTTPS, "QDRANT_HTTPS");

        // Embeddings
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_MODEL_NAME, "EMBEDDINGS_MODEL_NAME");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT, "EMBEDDINGS_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY, "EMBEDDINGS_CLIENT_API_KEY");

        // Chat Completions
        _variableNames.Add(EnvironmentVariable.CHAT_DEPLOYMENT_NAME, "CHAT_DEPLOYMENT_NAME");
        _variableNames.Add(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT, "AZURE_OPENAI_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY, "AZURE_OPENAI_CLIENT_API_KEY");

        // Document Intelligence
        _variableNames.Add(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT, "DOCUMENT_INTELLIGENCE_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.DOCUMENT_INTELLIGENCE_CLIENT_API_KEY, "DOCUMENT_INTELLIGENCE_CLIENT_API_KEY");
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)



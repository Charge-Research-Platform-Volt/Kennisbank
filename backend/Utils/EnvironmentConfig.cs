using System.Collections.Concurrent;

namespace KnowledgeBank.Utils;


public enum EnvironmentVariable
{
    ASPNETCORE_ENVIRONMENT,
    CONNECTION_STRING,
    AZURE_STORAGE_CONNECTION_STRING,
    HOST_URL,
    EMBEDDINGS_CLIENT_ENDPOINT,
    EMBEDDINGS_CLIENT_API_KEY,
    AZURE_OPENAI_CLIENT_ENDPOINT,
    AZURE_OPENAI_CLIENT_API_KEY,
    CHAT_COMPLETIONS_CLIENT_ENDPOINT,
    CHAT_COMPLETIONS_CLIENT_API_KEY,
}


public class EnvironmentConfig
{
    private readonly IConfiguration _configuration;
    private readonly ConcurrentDictionary<EnvironmentVariable, string> _variableValues = new();
    private readonly Dictionary<EnvironmentVariable, string> _variableNames = new();


    public EnvironmentConfig(IConfiguration configuration)
    {
        _configuration = configuration;

        // Initialize environment variable names
        _variableNames.Add(EnvironmentVariable.ASPNETCORE_ENVIRONMENT, "ASPNETCORE_ENVIRONMENT");
        _variableNames.Add(EnvironmentVariable.CONNECTION_STRING, "CONNECTION_STRING");
        _variableNames.Add(EnvironmentVariable.AZURE_STORAGE_CONNECTION_STRING, "AZURE_STORAGE_CONNECTION_STRING");
        _variableNames.Add(EnvironmentVariable.HOST_URL, "HOST_URL");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_CLIENT_ENDPOINT, "EMBEDDINGS_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.EMBEDDINGS_CLIENT_API_KEY, "EMBEDDINGS_CLIENT_API_KEY");
        _variableNames.Add(EnvironmentVariable.AZURE_OPENAI_CLIENT_ENDPOINT, "AZURE_OPENAI_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.AZURE_OPENAI_CLIENT_API_KEY, "AZURE_OPENAI_CLIENT_API_KEY");
        _variableNames.Add(EnvironmentVariable.CHAT_COMPLETIONS_CLIENT_ENDPOINT, "CHAT_COMPLETIONS_CLIENT_ENDPOINT");
        _variableNames.Add(EnvironmentVariable.CHAT_COMPLETIONS_CLIENT_API_KEY, "CHAT_COMPLETIONS_CLIENT_API_KEY");
    }


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
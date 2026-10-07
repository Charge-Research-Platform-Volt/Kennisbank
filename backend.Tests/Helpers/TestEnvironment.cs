using KnowledgeBank.Utils;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBank.Tests.Helpers;

/// <summary>
/// Builds an <see cref="EnvironmentConfig"/> with a dummy value for every variable, so services that
/// read their settings from it can be constructed in tests without a real .env.
/// </summary>
public static class TestEnvironment
{
    public static Dictionary<string, string?> DefaultValues() =>
        Enum.GetValues<EnvironmentVariable>().ToDictionary(v => v.ToString(), v => (string?)DefaultValue(v));

    public static EnvironmentConfig CreateConfig(Dictionary<string, string?>? values = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? DefaultValues())
            .Build();

        EnvironmentConfig config = new(configuration);
        config.CheckEnvironmentVariables();
        return config;
    }

    private static string DefaultValue(EnvironmentVariable variable)
    {
        string name = variable.ToString();
        if (name.EndsWith("_PER_MINUTE") || name.EndsWith("_MAX_CONCURRENT_REQUESTS")) return "1000";
        if (name.EndsWith("_ENDPOINT") || name.EndsWith("_URL")) return "http://localhost.test";
        if (name == nameof(EnvironmentVariable.S3_USE_SSL)) return "false";
        return $"test-{name.ToLowerInvariant()}";
    }
}

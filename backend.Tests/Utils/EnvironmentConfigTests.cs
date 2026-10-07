using KnowledgeBank.Tests.Helpers;
using KnowledgeBank.Utils;
using Microsoft.Extensions.Configuration;

namespace KnowledgeBank.Tests.Utils;

public class EnvironmentConfigTests
{
    [Fact]
    public void GetVariableValue_ReturnsConfiguredValue()
    {
        Dictionary<string, string?> values = TestEnvironment.DefaultValues();
        values[nameof(EnvironmentVariable.S3_BUCKET_NAME)] = "kennisbank";

        EnvironmentConfig config = TestEnvironment.CreateConfig(values);

        Assert.Equal("kennisbank", config.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME));
    }

    [Fact]
    public void GetVariableValue_BeforeCheck_Throws()
    {
        EnvironmentConfig config = new(new ConfigurationBuilder().AddInMemoryCollection(TestEnvironment.DefaultValues()).Build());

        Assert.Throws<ArgumentException>(() => config.GetVariableValue(EnvironmentVariable.HOST_URL));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CheckEnvironmentVariables_MissingOrEmptyVariable_ThrowsNamingIt(string? value)
    {
        Dictionary<string, string?> values = TestEnvironment.DefaultValues();
        values[nameof(EnvironmentVariable.MISTRAL_API_KEY)] = value;

        ArgumentException ex = Assert.Throws<ArgumentException>(() => TestEnvironment.CreateConfig(values));
        Assert.Contains(nameof(EnvironmentVariable.MISTRAL_API_KEY), ex.Message);
    }
}

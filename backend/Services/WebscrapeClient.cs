using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services;

public class WebscrapeClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient httpClient;
    private readonly Serilog.ILogger logger = Serilog.Log.ForContext<WebscrapeClient>();

    public WebscrapeClient(EnvironmentConfig environmentConfig)
    {
        Uri endpoint = new(environmentConfig.GetVariableValue(EnvironmentVariable.WEBSCRAPE_SERVICE_URL).TrimEnd('/') + "/");
        // Slightly longer timeout than webscrape's own timeout of 30s
        httpClient = new HttpClient { BaseAddress = endpoint, Timeout = TimeSpan.FromSeconds(35) };
    }

    public Task<WebscrapeResult> ExtractFromUrlAsync(string url)
        => PostAsync(new { url }, url);

    public Task<WebscrapeResult> ExtractFromHtmlAsync(string html)
        => PostAsync(new { html }, "uploaded HTML file");

    private async Task<WebscrapeResult> PostAsync(object body, string logLabel)
    {
        try
        {
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("extract", body);
            response.EnsureSuccessStatusCode();

            WebscrapeResult? result = await response.Content.ReadFromJsonAsync<WebscrapeResult>(JsonOptions);
            return result ?? new WebscrapeResult();
        }
        catch (Exception e)
        {
            logger.Error(e, "Webscrape service request failed for '{label}'", logLabel);
            return new WebscrapeResult();
        }
    }
}
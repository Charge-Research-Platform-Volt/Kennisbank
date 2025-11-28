using PuppeteerSharp;

namespace KnowledgeBank.Services;

public class BrowserService(ILogger<BrowserService> logger) : IAsyncDisposable 
{
    private IBrowser? browser;
    private string? readabilityScript;
    private readonly SemaphoreSlim browserLock = new (1, 1);
    private readonly SemaphoreSlim scriptLock = new(1, 1);
    private readonly HttpClient httpClient = new();
        
    public async Task<IBrowser> GetBrowserAsync()
    {
        if (browser != null) return browser;
        
        await browserLock.WaitAsync();
        
        try 
        {
            browser ??= await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = "/usr/bin/chromium",
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            return browser;
        }
        finally 
        {
            browserLock.Release();
        }
    }
    
    public async Task<string> GetReadabilityScriptAsync()
    {
        if (readabilityScript != null) return readabilityScript;

        await scriptLock.WaitAsync();
        try
        {
            if (readabilityScript != null) return readabilityScript;

            try
            {
                readabilityScript = await httpClient.GetStringAsync(
                    "https://cdn.jsdelivr.net/npm/@mozilla/readability/Readability.js"
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to fetch Readability.js from CDN: {message}. Using bundled fallback.", ex.Message);
                readabilityScript = await File.ReadAllTextAsync("Resources/Readability.js");
            }

            return readabilityScript;
        }
        finally
        {
            scriptLock.Release();
        }
    }

    public async ValueTask DisposeAsync() 
    {
        if (browser != null)
            await browser.CloseAsync();
    }
}
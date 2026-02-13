using System.Text;
using System.Text.Json;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services;

public class VPNService
{
    private readonly HttpClient httpClient;

    public VPNService(EnvironmentConfig environmentConfig)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(environmentConfig.GetVariableValue(EnvironmentVariable.HEADSCALE_URL) + "/api/v1/")
        };
        httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + environmentConfig.GetVariableValue(EnvironmentVariable.HEADSCALE_API_KEY));
    }

    public async Task<(string id, string name)[]> GetAllUsers()
    {
        var response = await httpClient.GetAsync("user");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("users").EnumerateArray()
            .Select(u => (u.GetProperty("id").GetString()!, u.GetProperty("name").GetString()!))
            .ToArray();
    }

    public async Task<string> CreateUser(string name)
    {
        StringContent content = new(JsonSerializer.Serialize(new { name }), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("user", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("user").GetProperty("id").GetString()!;
    }

    public async Task<string> GetUserId(string name)
    {
        var response = await httpClient.GetAsync($"user?name={name}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("user").GetProperty("id").GetString()!;
    }

    public async Task<string> GetPreAuthKey(string userId)
    {
        StringContent content = new(JsonSerializer.Serialize(new { user = userId, reusable = true, expiration = "9999-12-31T00:00:00Z" }), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("preauthkey", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("preAuthKey").GetProperty("key").GetString()!;
    }

    public async Task<string[]> GetUserNodes(string name)
    {
        var response = await httpClient.GetAsync($"node?user={name}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("nodes").EnumerateArray().Select(n => n.GetProperty("id").GetString()!).ToArray();
    }

    public async Task DeleteNodes(string[] nodeIds)
    {
        foreach (string nodeId in nodeIds)
        {
            var response = await httpClient.DeleteAsync($"node/{nodeId}");
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task DeleteUser(string userId)
    {
        var response = await httpClient.DeleteAsync($"user/{userId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserWithNodes(string name)
    {
        // Check if user exists, silently return if not
        var response = await httpClient.GetAsync($"user?name={name}");
        if (!response.IsSuccessStatusCode)
            return;
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string userId = json.GetProperty("user").GetProperty("id").GetString()!;

        // Delete nodes
        string[] nodeIds = await GetUserNodes(name);
        await DeleteNodes(nodeIds);

        // Delete user
        await DeleteUser(userId);
    }

    public async Task RenameUser(string userId, string newName)
    {
        var response = await httpClient.PostAsync($"user/{userId}/rename/{newName}", null);
        response.EnsureSuccessStatusCode();
    }
}

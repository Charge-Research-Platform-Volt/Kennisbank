using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace KnowledgeBank.IntegrationTests.Infrastructure;

/// <summary>
/// Local stand-in for the OpenAI-compatible embeddings API and the Mistral API, so tests never
/// reach a real (paid) service. Embeddings are deterministic: the same text always gets the same vector.
/// Mistral has no routes on purpose; any call to it fails with a 404.
/// </summary>
public sealed class FakeAiServer : IDisposable
{
    public const int Dimensions = 1024;

    private readonly WireMockServer server = WireMockServer.Start();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> failMarkers = new();

    public string EmbeddingsEndpoint => $"{server.Url}/embeddings/v1";
    public string MistralEndpoint => $"{server.Url}/mistral";
    public string Url => server.Url!;

    /// <summary>Number of embedding requests received so far.</summary>
    public int EmbeddingRequests { get; private set; }

    public FakeAiServer()
    {
        server
            .Given(Request.Create().WithPath("/embeddings/v1/embeddings").UsingPost())
            .RespondWith(Response.Create().WithCallback(request =>
            {
                EmbeddingRequests++;

                JsonNode body = JsonNode.Parse(request.Body!)!;
                string[] inputs = body["input"] is JsonArray array
                    ? [.. array.Select(n => n!.GetValue<string>())]
                    : [body["input"]!.GetValue<string>()];

                // A 500 isn't retried by EmbeddingService, so a failure test doesn't wait on backoff
                if (failMarkers.Keys.Any(marker => inputs.Any(input => input.Contains(marker))))
                    return new WireMock.ResponseMessage { StatusCode = 500, BodyData = Body("""{"error":{"message":"fake failure"}}""") };
                bool base64 = body["encoding_format"]?.GetValue<string>() == "base64";

                var data = inputs.Select((text, i) => new
                {
                    @object = "embedding",
                    index = i,
                    embedding = base64 ? (object)Convert.ToBase64String(ToBytes(VectorFor(text))) : VectorFor(text)
                });

                string json = JsonSerializer.Serialize(new
                {
                    @object = "list",
                    data,
                    model = body["model"]?.GetValue<string>() ?? "fake",
                    usage = new { prompt_tokens = inputs.Sum(t => t.Length / 4), total_tokens = inputs.Sum(t => t.Length / 4) }
                });

                return new WireMock.ResponseMessage
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, WireMock.Types.WireMockList<string>> { ["Content-Type"] = new("application/json") },
                    BodyData = Body(json)
                };
            }));
    }

    /// <summary>
    /// Makes embedding requests fail with a 500 while any of their texts contains <paramref name="marker"/>.
    /// Targeted on purpose: background ingestion from other tests keeps working normally.
    /// </summary>
    public void FailEmbeddingsContaining(string marker) => failMarkers[marker] = 0;

    public void StopFailing(string marker) => failMarkers.TryRemove(marker, out _);

    /// <summary>Deterministic unit vector derived from the text.</summary>
    public static float[] VectorFor(string text)
    {
        float[] vector = new float[Dimensions];
        byte[] seed = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        Random random = new(BitConverter.ToInt32(seed, 0));

        double norm = 0;
        for (int i = 0; i < Dimensions; i++)
        {
            vector[i] = (float)(random.NextDouble() * 2 - 1);
            norm += vector[i] * vector[i];
        }

        float scale = (float)(1 / Math.Sqrt(norm));
        for (int i = 0; i < Dimensions; i++) vector[i] *= scale;
        return vector;
    }

    private static byte[] ToBytes(float[] vector)
    {
        byte[] bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    private static WireMock.Util.BodyData Body(string json) => new()
    {
        BodyAsString = json,
        DetectedBodyType = WireMock.Types.BodyType.String,
        Encoding = Encoding.UTF8
    };

    public void Dispose() => server.Stop();
}

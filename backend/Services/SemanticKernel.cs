using Microsoft.Extensions.VectorData.Properties;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Qdrant.Client;
using Serilog;

#pragma warning disable SKEXP0070, SKEXP0001

namespace KnowledgeBank.Services;


public interface ISemanticKernel
{
    Kernel Kernel { get; }
    // QdrantVectorStore VectorStore { get; }
}



public class SemanticKernel : ISemanticKernel
{
    private readonly Kernel _kernel;
    // private readonly QdrantVectorStore _vectorStore;

    // public readonly ISemanticTextMemory _memory;
    private readonly Serilog.ILogger _logger;


    public SemanticKernel()
    {
        _logger = Log.ForContext<SemanticKernel>();
        _logger.Information("Initializing Semantic Kernel with Ollama and Qdrant");

        // Create Kernel builder
        IKernelBuilder builder = Kernel.CreateBuilder();

        // Add Ollama text embedding generation
        builder.AddOllamaTextEmbeddingGeneration(endpoint: new Uri("http://ollama:11434/"), modelId: "paraphrase-multilingual:latest");

        // Add Ollama chat completion
        builder.AddOllamaChatCompletion("gemma3:12b", new Uri("http://qdrant:11434"));

        builder.Services.AddQdrantVectorStore("localhost", 6333);

        // QdrantClient qdrantClient = new QdrantClient("localhost", 6333);
        // _vectorStore = new QdrantVectorStore(qdrantClient);

        // builder.Services.AddSingleton<IMemoryStore>(_vectorStore);

        // Build the kernel
        _kernel = builder.Build();

        // ---


        // Create Qdrant client
        // QdrantClient qdrantClient = new QdrantClient("localhost", 6333);

        // Create vector store using Qdrant
        // QdrantVectorStore vectorStore = new QdrantVectorStore(qdrantClient);

        // Get the embedding service from the kernel
        // Ix embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        // Create and initialize semantic text memory
        // _memory = new SemanticTextMemory(vectorStore, embeddingGenerator);



        _logger.Information("Semantic Kernel successfully initialized with Ollama and Qdrant");
    }
    public Kernel Kernel => _kernel;
    // public QdrantVectorStore VectorStore => _vectorStore;
}
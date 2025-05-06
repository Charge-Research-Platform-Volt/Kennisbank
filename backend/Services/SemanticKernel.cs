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
}



public class SemanticKernel : ISemanticKernel
{
    private readonly Kernel _kernel;
    // public ISemanticTextMemory _memory;

    // private readonly string _embeddingModelName;
    // private readonly Serilog.ILogger _logger;

    // public ISemanticTextMemory Memory => _memory;


    public SemanticKernel()
    {
        // _logger = Log.ForContext<SemanticKernel>();

        IKernelBuilder builder = Kernel.CreateBuilder();
        builder.AddOllamaTextEmbeddingGeneration(endpoint: new Uri("http://ollama:11434/"), modelId: "paraphrase-multilingual:latest");
        builder.AddOllamaChatCompletion("gemma3:12b", new Uri("http://qdrant:11434"));
        _kernel = builder.Build();


        // QdrantClient qdrantClient = new QdrantClient("http://localhost", 6333);
        // QdrantVectorStore qdrantVectorStore = new QdrantVectorStore(qdrantClient);


        // var embeddingGeneration = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        // _memory = new SemanticTextMemory(
        //        new MemoryBuilder()
        //            .WithTextEmbeddingGeneration(embeddingGeneration)
        //            .WithVectorStore(qdrantVectorStore)
        //            .Build()
        //    );




        // _kernel = builder.Build();


        // _logger.Information("Initializing Semantic Kernel with Ollama endpoint: {OllamaEndpoint} and Qdrant endpoint: {QdrantEndpoint}", ollamaEndpoint, qdrantEndpoint);
    }
    public Kernel Kernel => _kernel;


    // public SemanticKernel(IKernelConfig kernelConfig)
    // {
    //     _kernel = new KernelBuilder()
    //         .WithMemory(kernelConfig.Memory)
    //         .WithAIService(kernelConfig.AIService)
    //         .WithSkillCollection(kernelConfig.SkillCollection)
    //         .Build();
    // }

}
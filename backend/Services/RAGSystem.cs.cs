using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Serilog;

#pragma warning disable SKEXP0070, SKEXP0001

namespace KnowledgeBank.Services;


public interface IRAGSystem
{
    Kernel Kernel { get; }
    IVectorStore VectorStore { get; }
    ITextEmbeddingGenerationService EmbeddingGenerator { get; }
    IChatCompletionService ChatCompletionService { get; }
    Task MainPipeline(Guid resourceId, string fileType, FileResourceCreateDto resourcMetaData);
    IVectorStoreRecordCollection<Guid, ResourceVectorStoreRecord> Collection { get; }
    VectorStoreTextSearch<ResourceVectorStoreRecord> TextSearch { get; }
}


public class RAGSystem : IRAGSystem
{
    private readonly Serilog.ILogger _logger;

    private readonly Kernel _kernel;
    private readonly ITextEmbeddingGenerationService _embeddingGenerator;
    private readonly IVectorStore _vectorStore;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly IVectorStoreRecordCollection<Guid, ResourceVectorStoreRecord> _collection;
    private readonly VectorStoreTextSearch<ResourceVectorStoreRecord> _textSearch;


    // Services:
    private readonly IAzureBlobService _blobService;
    private readonly ITools _toolbox;


    // Constants:
    public const string COLLECTION_NAME = "Knowledgebank";
    public const int EMBEDDING_DIMENSIONS = 768; // 768 for gemma3:4b
    // private Uri OLLAMA_ENDPOINT = new Uri("http://localhost:11434/"); //docker ollama
    private Uri OLLAMA_ENDPOINT = new Uri("http://host.docker.internal:11434/"); //local ollama running on macOS


    public Kernel Kernel => _kernel;
    public IVectorStore VectorStore => _vectorStore;
    public ITextEmbeddingGenerationService EmbeddingGenerator => _embeddingGenerator;
    public IChatCompletionService ChatCompletionService => _chatCompletionService;
    public IVectorStoreRecordCollection<Guid, ResourceVectorStoreRecord> Collection => _collection;
    public VectorStoreTextSearch<ResourceVectorStoreRecord> TextSearch => _textSearch;


    public RAGSystem(IAzureBlobService blobService)
    {
        _logger = Log.ForContext<RAGSystem>();
        _blobService = blobService;

        _logger.Information("Initializing RAG system");

        _toolbox = new Tools();
        _logger.Information("Tools successfully initialized");


        // *************** Semantic Kernel ***************
        _logger.Information("Initializing Semantic Kernel with Ollama and Qdrant");

        // Create Kernel builder
        IKernelBuilder builder = Kernel.CreateBuilder();

        // Add Ollama text embedding generation
        builder.AddOllamaTextEmbeddingGeneration(endpoint: OLLAMA_ENDPOINT, modelId: "paraphrase-multilingual:latest");

        // Add Ollama chat completion
        builder.AddOllamaChatCompletion(endpoint: OLLAMA_ENDPOINT, modelId: "gemma3:4b");
        builder.Services.AddQdrantVectorStore(host: "qdrant");

        // Build the kernel
        _kernel = builder.Build();
        _logger.Information("Semantic Kernel successfully initialized with Ollama and Qdrant");


        // *************** Services ***************
        // -- Initialize chat completion service:
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
        _logger.Information("Chat completion service successfully initialized");

        // -- Initialize the embedding generator:
        _embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _logger.Information("Text embedding generation service successfully initialized");

        // -- Initialize the Qdrant vector store:
        _vectorStore = _kernel.Services.GetRequiredService<IVectorStore>();
        _logger.Information("Qdrant vector store successfully initialized");

        // -- Initialize the Qdrant collection:
        _collection = _vectorStore.GetCollection<Guid, ResourceVectorStoreRecord>(COLLECTION_NAME);
        _logger.Information("Collection {CollectionName} successfully initialized", COLLECTION_NAME);

        // Initialize collection asynchronously
        InitializeCollectionAsync().GetAwaiter().GetResult();
        _logger.Information("Collection {CollectionName} successfully created", COLLECTION_NAME);


        _textSearch = new VectorStoreTextSearch<ResourceVectorStoreRecord>(_collection, _embeddingGenerator);


        // Create options to describe the function I want to register.
        var options = new KernelFunctionFromMethodOptions()
        {
            FunctionName = "Search",
            Description = "Perform a search for content related to the specified query from a record collection.",
            Parameters =
            [
                new KernelParameterMetadata("query") { Description = "What to search for", IsRequired = true },
                new KernelParameterMetadata("top") { Description = "Number of results", IsRequired = false, DefaultValue = 10 },
                new KernelParameterMetadata("skip") { Description = "Number of results to skip", IsRequired = false, DefaultValue = 0 },
            ],

            ReturnParameter = new() { ParameterType = typeof(KernelSearchResults<string>) },
        };

        var searchPlugin = _textSearch.CreateWithGetTextSearchResults("SearchPlugin");
        // var searchPlugin = KernelPluginFactory.CreateFromFunctions("SearchPlugin", "Perform a search for content related to the specified query from a record collection.", [_textSearch.CreateGetTextSearchResults(options)]);
        _kernel.Plugins.Add(searchPlugin);


        _logger.Information("RAG system successfully initialized");
    }



    /// <summary>
    /// Initializes the Qdrant (vector database) collection if it does not exist.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeCollectionAsync()
    {
        await _collection.CreateCollectionIfNotExistsAsync();
    }



    /// <summary>
    /// Processes a document through the RAG (Retrieval-Augmented Generation) pipeline.
    /// </summary>
    /// <param name="resourceId">The unique identifier for the resource being processed.</param>
    /// <param name="fileType">The type of file being processed (e.g., PDF, DOCX).</param>
    /// <param name="resourcMetaData">Metadata associated with the file resource.</param>
    /// <returns>A task representing the asynchronous operation of processing the document.</returns>
    /// <remarks>
    /// The pipeline consists of the following steps:
    /// 1. Extract text from the document
    /// 2. Chunk the extracted text into smaller segments
    /// 3. Generate vector embeddings for each chunk
    /// 4. Store the chunks and their embeddings in the vector database
    /// 
    /// If no text is extracted from the document, the process will terminate early.
    /// </remarks>
    public async Task MainPipeline(Guid resourceId, string fileType, FileResourceCreateDto resourcMetaData)
    {
        // -- Extract text from the document
        string extractedText = await _toolbox.ExtractTextAsync(fileType, resourceId, _blobService);

        if (string.IsNullOrEmpty(extractedText))
        {
            _logger.Warning("No text extracted from the document");
            return;
        }


        // -- Chunk the extracted text
        List<string> chunks = _toolbox.SplitTextIntoChunks(extractedText, true);


        // -- Generate embeddings for the chunks
        IList<ReadOnlyMemory<float>> embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(chunks, _kernel);
        _logger.Warning("Embeddings generated for {Count} chunks", embeddings.Count);


        // -- Store the chunks and embeddings in the vector store
        foreach (var chunk in chunks)
        {
            var embedding = embeddings[chunks.IndexOf(chunk)];

            var em = new ResourceVectorStoreRecord
            {
                Id = Guid.NewGuid(),
                ResourceId = resourceId.ToString(),
                ChunkType = ChunkType.ContentText.ToString(),
                ChunkText = chunk,
                ChunkEmbedding = embedding,

                // Description = chunk,
                // Tags = ["pdf", "chunk"],
                // Text2 = "hello fron full text search",
                // Embedding = embedding,
                // ExtraField = "extra field",
                // Embedding = embedding
            };

            await _collection.UpsertAsync(em);
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


// ------- notes

// ReadOnlyMemory<float> searchVector = await _ragSystem.EmbeddingGenerator.GenerateEmbeddingAsync("what is Charge?");

// // Do the search.
// var searchResult = _ragSystem.Collection.SearchEmbeddingAsync(searchVector, top: 10);

// // Inspect the returned hotel.
// await foreach (var record in searchResult)
// {
//     _logger.Information("Found hotel description: " + record.Record.ChunkText);
//     _logger.Information("Found hotel chunk type: " + record.Record.ChunkType);
//     _logger.Information("Found record score: " + record.Score);
// }



// KernelSearchResults<TextSearchResult> textResults = await _ragSystem.TextSearch.GetTextSearchResultsAsync(query, new() { Top = 2, Skip = 0 });
// Console.WriteLine("\n--- Text Search Results ---\n");
// await foreach (TextSearchResult result in textResults.Results)
// {
//     Console.WriteLine($"Name:  {result.Name}");
//     Console.WriteLine($"Value: {result.Value}");
// }

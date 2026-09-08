// =========================================================================
// POC 3: RAG over Your Own Logs (Ollama + Semantic Kernel Vector Data)
//
// The AI answers questions using YOUR data (mock past incidents) instead
// of guessing from its training data. This combines:
//   1. An embedding model (nomic-embed-text) that turns text into vectors
//   2. An in-memory vector store that holds those vectors
//   3. A chat model (llama3.1:8b) that writes the final answer, grounded
//      in whatever relevant incidents were retrieved
//
// Both the chat model and the embedding model are accessed through the
// SAME OllamaApiClient class (from OllamaSharp) - it implements both
// IChatClient and IEmbeddingGenerator<string, Embedding<float>> natively,
// so we don't need Semantic Kernel's separate (and currently buggy)
// dedicated Ollama connector for either of these.
//
// NOTE ON A COMMON GOTCHA:
// IEmbeddingGenerator.GenerateAsync(IEnumerable<string> values) returns a
// GeneratedEmbeddings<Embedding<float>> COLLECTION - but if you pass it a
// single bare string, C# instead resolves to a different "accelerator"
// extension overload that returns a single Embedding<float> directly
// (NOT a collection), so trying to index it with [0] fails to compile.
// The cleanest fix - used throughout this file - is to call the dedicated
// GenerateVectorAsync(string) extension method instead, which returns a
// ReadOnlyMemory<float> directly.
//
// ANOTHER GOTCHA: ReadOnlyMemory<float> itself has NO [] indexer either -
// only Span<T> and arrays do. So even after switching to
// GenerateVectorAsync, if you ever need to read a single element out of
// the resulting vector, use either:
//     vector.Span[0]        (no extra allocation)
//     vector.ToArray()[0]   (simplest, small allocation)
// You do NOT need to index the vector anywhere in the flow below - it's
// only ever assigned whole into IncidentRecord.Embedding or passed whole
// into SearchAsync - but it's worth remembering if you extend this code.
// =========================================================================

using CommunityToolkit.VectorData.InMemory;
using DotNetEnv;
using Microsoft.Extensions.AI;
using OllamaSharp;

Env.Load();

var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
var chatModel = Environment.GetEnvironmentVariable("OLLAMA_CHAT_MODEL") ?? "llama3.1:8b";
var embedModel = Environment.GetEnvironmentVariable("OLLAMA_EMBED_MODEL") ?? "nomic-embed-text";

Console.WriteLine(Environment.GetEnvironmentVariable("OLLAMA_CHAT_MODEL"));
IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = 
    new OllamaApiClient(new Uri(endpoint), embedModel);

IChatClient chatClient =
    new OllamaApiClient(new Uri(endpoint), chatModel);

Console.WriteLine("Testing embedding generation...");
// Use GenerateVectorAsync (returns ReadOnlyMemory<float> directly) instead
// of GenerateAsync (which, for a single string input, resolves to an
// overload returning a bare Embedding<float> that can't be indexed).
var testEmbedding = await embeddingGenerator.GenerateVectorAsync("The store's POS terminal is offline");

Console.WriteLine($" Vector Length: {testEmbedding.Span[0]}");
Console.WriteLine($" Firstd 5 values: {string.Join(", ", testEmbedding.ToArray().Take(5))}\n");

// NOTE: if the printed vector length is NOT 768, update the Dimensions
// value in IncidentRecord.cs to match, or ingestion/search below may fail.

// -------------------------------------------------------------------
// MILESTONE 4 (part 1): Set up the in-memory vector store and collection.
// -------------------------------------------------------------------

var vectorStore = new InMemoryVectorStore();
var collection = vectorStore.GetCollection<string, IncidentRecord>("incidents");
await collection.EnsureCollectionExistsAsync();

// -------------------------------------------------------------------
// MILESTONE 4 (part 2): Ingest the mock incident data.
// For each incident: generate its embedding, build an IncidentRecord,
// and upsert it into the collection.
//
// MockIncidentData is a separate static class (see MockIncidentData.cs)
// holding a hardcoded List<(string Id, string Text)> of fictional past
// incidents - standing in for a real Application Insights/ticket export.
// -------------------------------------------------------------------
Console.WriteLine($"Ingesting {MockIncidentData.Incidents.Count} mock incident records...");

foreach (var (id, text) in MockIncidentData.Incidents)
{
    // GenerateVectorAsync returns ReadOnlyMemory<float> directly - assign
    // straight into the record, no indexing required.
    ReadOnlyMemory<float> vector = await embeddingGenerator.GenerateVectorAsync(text);

    var record = new IncidentRecord
    {
        Id = id,
        Text = text,
        Embedding = vector
    };

    await collection.UpsertAsync(record);
}

Console.WriteLine("Ingestion complete.\n");

// -------------------------------------------------------------------
// Helper: given a user question, retrieve the top-K most relevant
// incidents and build a context block to inject into the prompt.
// This is MILESTONE 5 (search) + half of MILESTONE 6 (context building).
// -------------------------------------------------------------------

async Task<string> RetrieveContextAsync(string question, int topK = 3)
{
    ReadOnlyMemory<float> queryVector = await embeddingGenerator.GenerateVectorAsync(question);

    var results = collection.SearchAsync(queryVector, top: topK);

    var contextLines = new List<string>();
    int i = 1;
    await foreach (var result in results)
    {
        contextLines.Add($"{i}. (similarity {result.Score:F3}) {result.Record.Text}");
        i++;
    }

    return contextLines.Count > 0
        ? string.Join("\n", contextLines)
        : "(no relevant incidents found)";
}

// -------------------------------------------------------------------
// MILESTONE 6 (rest) + MILESTONE 7: build the full RAG prompt and wrap
// it in an interactive chat loop.
// -------------------------------------------------------------------

Console.WriteLine("=========================================");
Console.WriteLine(" RAG Incident Bot (Ollama, local, free)");
Console.WriteLine("=========================================");
Console.WriteLine("Ask about a technical issue - the bot will search past");
Console.WriteLine("incidents and ground its answer in what it finds.");
Console.WriteLine("Type 'exit' to quit.\n");

while (true)
{
    Console.Write("You: ");
    var question = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(question) || question.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var context = await RetrieveContextAsync(question);

    // This is the actual "augmentation" step of Retrieval-Augmented
    // Generation: we hand the model retrieved real data and instruct it
    // to ground its answer in that data rather than inventing one.
    var augmentedPrompt =
        $"""
        You are a technical support assistant. Use the following past
        incident records to help answer the user's question. If none of
        the incidents seem relevant, say so honestly instead of guessing.

        Similar past incidents:
        {context}

        User question: {question}
        """;

    Console.Write("AI:  ");
    await foreach (var update in chatClient.GetStreamingResponseAsync(augmentedPrompt))
    {
        Console.Write(update.Text);
    }
    Console.WriteLine("\n");
}

Console.WriteLine("Goodbye!");

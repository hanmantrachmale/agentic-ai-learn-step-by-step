using DotNetEnv;
using Microsoft.Extensions.AI;
using OpenAI;
using OllamaSharp;
Console.WriteLine("Hello, World!");


Env.Load();
string provider = Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "openai";

IChatClient chatClient = provider switch
{
    "openai" => BuildOpenAIChatProvider(),
    "azure" => BuildAzureChatProvider(),
    "ollama" => BuildOllamaChatProvider(),
    _ => throw new NotSupportedException($"The AI_PROVIDER '{provider}' is not supported. Use 'openai', 'azure', or 'ollama'.")
};

var history = new List<ChatMessage>
{
    new ChatMessage(ChatRole.System, "You are a helpful assistant.")
};

while (true)
{
    Console.Write("User: ");
    string userInput = Console.ReadLine() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(userInput))
    {
        Console.WriteLine("Exiting...");
        break;
    }

    history.Add(new ChatMessage(ChatRole.User, userInput));

    var updates = new List<ChatResponseUpdate>();

    await foreach (var update in chatClient.GetStreamingResponseAsync(history))
    {
       Console.Write(update.Text);
       updates.Add(update);
    }

    history.AddMessages(updates);
    Console.WriteLine();
}

IChatClient BuildOllamaChatProvider()
{
    var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434";
    var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";
    return new OllamaApiClient(endpoint, model);
}

IChatClient BuildAzureChatProvider()
{
    // var azureAdTokenProvider = new DefaultAzureCredential();
    // var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT environment variable is not set.");
    // return new AzureChatClient(endpoint, azureAdTokenProvider);
    throw new NotImplementedException();
}

IChatClient BuildOpenAIChatProvider()
{
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
                    throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
    //var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "https://api.openai.com/v1";
    var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o";
    return new OpenAIClient(apiKey).GetChatClient(model).AsIChatClient();
}
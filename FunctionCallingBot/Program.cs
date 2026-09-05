using DotNetEnv;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Superpower.Model;



Env.Load();

Console.WriteLine("Hello, World!");

string provider = (Environment.GetEnvironmentVariable("AI_PROVIDER") ?? "OpenAI").ToLowerInvariant();

switch (provider)
{
    case "openai":
        Console.WriteLine("Using OpenAI as the AI provider.");
        break;
    case "azure":
        Console.WriteLine("Using Azure as the AI provider.");
        break;
    default:
        Console.WriteLine($"Unknown AI provider: {provider}. Defaulting to OpenAI.");
        break;
}

// -------------------------------------------------------------------------------------------
// STEP 1: Build the Kernel AI client based on the provider
// The Kernel is Semanctic Kernel's dependency injection container that manages AI clients and other services.
// everything AI-related: the model connection, plugins, loggins, etc. is managed by the Kernel.
// -------------------------------------------------------------------------------------------
var kernelBuilder = provider switch
{
    "openai" => BuildOpenAIKernel(),
    "ollama" => BuildOllamaKernel(),
    _ => throw new NotSupportedException($"The AI_PROVIDER '{provider}' is not supported. Use 'openai' or 'ollama'.")
};

// -------------------------------------------------------------------------------------------
// STEP 2: Register our plugins AND the logging filter.
// AddFromType<T>() uses reflection to find all the methods in the class T that are decorated with the SKFunction attribute and registers them as plugins in the kernel.
// The logging filter is a middleware that logs the input and output of each plugin call. It is useful for debugging and understanding how the AI model is being used.
// -------------------------------------------------------------------------------------------

kernelBuilder.Plugins.AddFromType<StoreLookupPlugin>("StoreLookup");


// -------------------------------------------------------------------------------------------
// STEP 4: Build the kernel and run the AI model
// -------------------------------------------------------------------------------------------

Kernel kernel = kernelBuilder.Build();

// -------------------------------------------------------------------------------------------
// STEP 5: Get the chat completion service and set up conversation state.
// ChatHistory is Semantic Kernel's version of the List<ChatMessage> that is used to store the conversation history. It is used to provide context to the AI model for generating responses.
// -------------------------------------------------------------------------------------------

var chatService = kernel.GetRequiredService<IChatCompletionService>();

var history = new ChatHistory();
history.AddUserMessage("You are a helpful assistant that can look up store information based on store codes. You have access to a plugin called StoreLookup that can get the POS ID, status, and manager name of stores.");


// -------------------------------------------------------------------------------------------
// STEP 6: This is the ONE setting that turns on function calling.
// FunctionChoiceBehavior.Auto() means: "Model, here are some tools - 
// decide for yourself whether and when to call them."
// -------------------------------------------------------------------------------------------

var executionSettings = new OpenAIPromptExecutionSettings
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};


while (true)
{
    Console.Write("User: ");
    string userInput = Console.ReadLine() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting...");
        break;
    }

    history.AddUserMessage(userInput);

    var response = await chatService.GetChatMessageContentAsync(history, executionSettings, kernel);

    Console.WriteLine($"Assistant: {response}\n");
    history.AddMessage(response.Role, response.Content ?? string.Empty);

}

Console.WriteLine("Goodbye!");

IKernelBuilder BuildOllamaKernel()
{
    var endpoint = Environment.GetEnvironmentVariable("OLLAMA_ENDPOINT") ?? "http://localhost:11434/v1";
    var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.2";
    var apiKey = Environment.GetEnvironmentVariable("OLLAMA_DUMMY_KEY") ?? 
    throw new InvalidOperationException("OLLAMA_API_KEY environment variable is not set.");
    var builder = Kernel.CreateBuilder()
                        .AddOpenAIChatCompletion(model, new Uri(endpoint), apiKey);
    return builder;
}

IKernelBuilder BuildOpenAIKernel()
{
    var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? "https://api.openai.com/v1";
    var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo";
    var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? 
    throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
    var builder = Kernel.CreateBuilder()
                        .AddOpenAIChatCompletion(model, new Uri(endpoint), apiKey);
    return builder;
}
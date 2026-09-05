# 1. What is POC2's Objective?

In plain English: you're upgrading POC1's chatbot so it can now do things, not just talk. You ask "What's the POS ID for store ST001?" and instead of guessing or making something up, the AI decides on its own to call one of your C# methods to fetch the real answer, then explains it back to you in natural language.

# Why this matters: 
This is the exact mechanism behind every "AI agent" you'll ever build — support bots, coding assistants, Copilot Studio agents. The AI never directly touches your database or API; it just decides when to call your code and what arguments to pass. This POC isolates that decision-making mechanism in its simplest form.

# 2. The AI Tech We're Using — Explained

## a) What is Semantic Kernel, and why not just IChatClient from POC1?

IChatClient (POC1) only knows how to send/receive text. Semantic Kernel is a higher-level framework built on top of that same idea, adding orchestration — specifically, the ability to advertise your C# methods as "tools" the model can choose to invoke.

## b) What is a "plugin"?

A plugin is just a plain C# class. No special base class or interface needed — Semantic Kernel uses reflection to scan the class for methods marked [KernelFunction]. In our project, StoreLookupPlugin.cs is the plugin — it has 4 methods (GetStorePosId, GetStoreManager, GetStoreStatus, ListAllStores) backed by a mock in-memory dictionary standing in for a real YOOBIC API call.

## c) What is [KernelFunction] and [Description]?

```
C#
[KernelFunction("get_store_pos_id")]
[Description("Gets the POS (point of sale) system ID for a given store code, e.g. ST001.")]

public string GetStorePosId([Description("The store code, e.g. ST001")] string storeCode)
```
[KernelFunction] marks the method as callable by the AI. [Description] is not just a comment — it's literally sent to the model as part of the tool definition. The model reads these descriptions to decide whether and how to call your function. Vague descriptions = the model won't call it, or will call it wrong.

## d) What is FunctionChoiceBehavior.Auto()?

This is the one setting that turns the feature on:
```
C#
var executionSettings = new OpenAIPromptExecutionSettings {
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
```

It tells the model: "here's a list of tools — you decide for yourself whether to call zero, one, or several of them". There are two other modes worth knowing about: Required() (forces a call) and None() (just describes what it would call, without calling it) — useful later for testing/dry-runs.


## e) What actually happens behind the scenes (the full loop)?
You ask:"What's the POS ID for store ST001?"
Semantic Kernel sends your message plus the list of available tools (with their descriptions) to the model.
The model replies with a structured "please call get_store_pos_id with storeCode=ST001" instead of plain text.
Semantic Kernel intercepts that, actually runs your C# method, and gets "POS-AU-1001".
That result gets sent back to the model.
The model uses it to write the final natural-language answer:"The POS ID for store ST001 is POS-AU-1001."

All 6 steps happen inside one call to GetChatMessageContentAsync.

## f) What's the FunctionLoggingFilter for?

This is an IFunctionInvocationFilter — a hook that lets you "wrap" every function call with your own code (logging, timing, validation). I added it purely so you can see step 3–4 happening live in the console, in yellow text, instead of it being invisible magic.

# 3. Step-by-Step Implementation

Step 1 — Scaffold + install packages
```
dotnet new console -o FunctionCallingBot
cd FunctionCallingBot
dotnet add package Microsoft.SemanticKernel
dotnet add package DotNetEnv
```
Step 2 — Copy in the 3 code files (Program.cs, StoreLookupPlugin.cs, FunctionLoggingFilter.cs) plus .env.example → rename to .env and add your OpenAI key.

Step 3 — Build the Kernel and register the plugin (this is the new part vs. POC1):
```
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(model, apiKey);
builder.Plugins.AddFromType<StoreLookupPlugin>("StoreLookup");
builder.Services.AddSingleton<IFunctionInvocationFilter, FunctionLoggingFilter>();
Kernel kernel = builder.Build();
```
Step 4 — Chat loop — nearly identical to POC1, just using ChatHistory instead of List<ChatMessage>, and passing executionSettings + kernel into the call so the model knows about your tools.

Step 5 — Run it:
dotnet run

# 4. How to Test It
Try asking...	                       | Expected behavior
"What's the POS ID for store ST001?"   | Triggers get_store_pos_id, returns POS-AU-1001
"Who manages ST002?"	               | Triggers get_store_manager, returns Kingsley Basker
"Is ST003 active?"	                   | Triggers get_store_status, returns Suspended
"List all stores"	| Triggers list_all_stores
"What's the capital of France?"	No function call — proves the model only calls tools when relevant
"POS ID for ST999"	Function still gets called, returns a "not found" message, model relays it naturally


Watch for the yellow [Model is calling function] lines — that's your proof the AI made an autonomous decision.

5. Real-World Use Cases
Direct upgrade path: swap the mock dictionary in StoreLookupPlugin.cs for real HttpClient calls to your YOOBIC API — nothing else in the code changes.
Internal support bot: franchisees or support staff ask questions in plain English; the bot silently calls the right backend system (YOOBIC, SQL Server, Application Insights) instead of users needing to know which system/API to query.
Multi-tool agents: add more plugin classes (e.g., SqlLookupPlugin, AppInsightsPlugin) — the model will pick the right tool across all of them automatically.
Foundation for POC5/POC6: this exact pattern — a plugin with [KernelFunction] methods — is what you'll later expose through Copilot Studio or MCP.

Ready for POC3 (RAG over your own logs) whenever you want — same depth of walkthrough, and it builds directly on the Kernel/plugin concepts you just learned here.
// =========================================================================
// FunctionLoggingFilter.cs
//
// This is optional but VERY educational: it hooks into Semantic Kernel's
// pipeline so that every time the AI model decides to call one of our
// plugin functions, we print that decision to the console BEFORE and
// AFTER it runs. This lets you literally watch the model's reasoning path:
// "user asked X -> model chose to call function Y with argument Z -> got
// result -> used it to answer".
// =========================================================================

using Microsoft.SemanticKernel;

public class FunctionLoggingFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var argsText = string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}"));
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"\n  [Model is calling function] {context.Function.PluginName}.{context.Function.Name}({argsText})");
        Console.ResetColor();

        // "next" actually runs the function. We could inspect/modify
        // context.Result before or after this call if we wanted to.
        await next(context);

        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  [Function returned] {context.Result}\n");
        Console.ResetColor();
    }
}

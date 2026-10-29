using System.Diagnostics;
using System.Diagnostics.Metrics;
using AiMultiAgent.Application.Interfaces;

namespace AiMultiAgent.Application;

public class OrchestratorAgent(OpenAIAgent openAIAgent, GeminiAgent geminiAgent) : IOrchestratorAgent
{
    internal const string OpenAiProvider = "openai";
    internal const string GeminiProvider = "gemini";

    private static readonly string[] ProgrammingKeywords =
    [
        "c#", "java", "python", "código", "programação",
        "api", "classe", "método", "função", "algoritmo"
    ];

    private static readonly string[] ExplanationKeywords =
    [
        "explique", "resuma", "o que é", "conceito",
        "definição", "história", "como funciona"
    ];

    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.Application");
    private static readonly Meter Meter = new("AiMultiAgent.Application");
    private static readonly Counter<long> RoutingCounter = Meter.CreateCounter<long>("ai_orchestrator_routing_total");

    public async Task<string> ProcessAsync(string prompt)
    {
        var provider = SelectProvider(prompt);

        using var activity = ActivitySource.StartActivity("orchestrator.process", ActivityKind.Internal);
        RoutingCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
        activity?.SetTag("ai.provider", provider);

        return provider == OpenAiProvider
            ? await openAIAgent.AskSync(prompt)
            : await geminiAgent.AskSync(prompt);
    }

    internal static string SelectProvider(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        var text = prompt.ToLowerInvariant();

        if (ContainsAnyKeyword(text, ProgrammingKeywords))
            return OpenAiProvider;

        if (ContainsAnyKeyword(text, ExplanationKeywords))
            return GeminiProvider;

        return GeminiProvider;
    }

    private static bool ContainsAnyKeyword(string text, IEnumerable<string> keywords)
    {
        return keywords.Any(text.Contains);
    }
}

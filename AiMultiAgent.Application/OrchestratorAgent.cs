using System.Diagnostics;
using System.Diagnostics.Metrics;
using AiMultiAgent.Application.Interfaces;

namespace AiMultiAgent.Application;

public class OrchestratorAgent(OpenAIAgent openAIAgent, GeminiAgent geminiAgent) : IOrchestratorAgent
{
    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.Application");
    private static readonly Meter Meter = new("AiMultiAgent.Application");
    private static readonly Counter<long> RoutingCounter = Meter.CreateCounter<long>("ai_orchestrator_routing_total");

    public async Task<string> ProcessAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        using var activity = ActivitySource.StartActivity("orchestrator.process", ActivityKind.Internal);

        var text = prompt.ToLowerInvariant();

        var programmingKeywords = new[]
        {
                "c#", "java", "python", "código", "programação",
                "api", "classe", "método", "função", "algoritmo"
            };

        var explanationKeywords = new[]
        {
                "explique", "resuma", "o que é", "conceito",
                "definição", "história", "como funciona"
            };

        var isProgramming = programmingKeywords.Any(k => text.Contains(k));
        var isExplanation = explanationKeywords.Any(k => text.Contains(k));

        if (isProgramming)
        {
            const string provider = "openai";
            RoutingCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
            activity?.SetTag("ai.provider", provider);
            return await openAIAgent.AskSync(prompt);
        }

        if (isExplanation)
        {
            const string provider = "gemini";
            RoutingCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
            activity?.SetTag("ai.provider", provider);
            return await geminiAgent.AskSync(prompt);
        }

        {
            const string provider = "gemini";
            RoutingCounter.Add(1, new KeyValuePair<string, object?>("provider", provider));
            activity?.SetTag("ai.provider", provider);
            return await geminiAgent.AskSync(prompt);
        }
    }
}

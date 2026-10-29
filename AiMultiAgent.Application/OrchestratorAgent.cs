using AiMultiAgent.Application.Interfaces;

namespace AiMultiAgent.Application;

public class OrchestratorAgent(OpenAIAgent openAIAgent, GeminiAgent geminiAgent) : IOrchestratorAgent
{
    public async Task<string> ProcessAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

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
            return await openAIAgent.AskSync(prompt);

        if (isExplanation)
            return await geminiAgent.AskSync(prompt);

        return await geminiAgent.AskSync(prompt);
    }
}

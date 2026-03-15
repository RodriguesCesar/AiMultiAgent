using AiMultiAgent.Application.Interfaces;
using Mscc.GenerativeAI.Types;

namespace AiMultiAgent.Application;

public class OrchestratorAgent : IOrchestratorAgent
{
    private readonly OpenAIAgent _openAIAgent;
    private readonly GeminiAgent _geminiAgent;

    public OrchestratorAgent()
    {
        _openAIAgent = new OpenAIAgent();
        _geminiAgent = new GeminiAgent();
    }
    public async Task<string> ProcessAsync(string prompt)
    {
        var text = prompt.ToLower();

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

        bool isProgramming = programmingKeywords.Any(k => text.Contains(k));
        bool isExplanation = explanationKeywords.Any(k => text.Contains(k));

        if (isProgramming)
            return await _openAIAgent.AskSync(prompt);
        else if (isExplanation)
            return await _geminiAgent.AskSync(prompt);
        else
            return await _geminiAgent.AskSync(prompt);
    }
}

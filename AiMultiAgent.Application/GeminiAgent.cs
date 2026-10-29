using Mscc.GenerativeAI;

namespace AiMultiAgent.Application;

/// <summary>
/// Classe para integração com o LLM da Gemini, da Google.
/// </summary>
public class GeminiAgent(string apiKey, string model)
{
    private readonly string _apiKey = apiKey;
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;

    public async Task<string> AskSync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API Key não configurada. Use LLM__Gemini__ApiKey.");

        var googleAI = new GoogleAI(_apiKey);
        var llm = googleAI.GenerativeModel(_model);
        var response = await llm.GenerateContent(prompt);

        return response.Text ?? string.Empty;
    }
}

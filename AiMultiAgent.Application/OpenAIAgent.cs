using OpenAI.Chat;

namespace AiMultiAgent.Application;

/// <summary>
/// Classe para integração com o LLM da OpenAI.
/// </summary>
public class OpenAIAgent(string apiKey, string model)
{
    private readonly string _apiKey = apiKey;
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4.1-mini" : model;

    public async Task<string> AskSync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenAI API Key não configurada. Use LLM__OpenAI__ApiKey.");

        var client = new ChatClient(
             model: _model,
             apiKey: _apiKey
         );

        var response = await client.CompleteChatAsync(prompt);

        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
}

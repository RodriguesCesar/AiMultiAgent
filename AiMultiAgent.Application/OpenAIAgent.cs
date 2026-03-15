
using OpenAI.Chat;

namespace AiMultiAgent.Application;

/// <summary>
/// Classe para integração com o LLM da OpenAI.
/// </summary>
public class OpenAIAgent
{
    public async Task<string> AskSync(string prompt)
    {
        var client = new ChatClient(
             model: "gpt-4.1-mini",
             apiKey: "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"
         );

        var response = await client.CompleteChatAsync(prompt);

        return response.Value.Content[0].Text;
    }
}

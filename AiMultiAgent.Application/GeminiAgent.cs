using Mscc.GenerativeAI;
using Mscc.GenerativeAI.Types;

namespace AiMultiAgent.Application;

/// <summary>
/// Clase para integração com o LLM da Gemini, da Google. 
/// </summary>
public class GeminiAgent
{
    public async Task<string> AskSync(string prompt)
    {
        var googleAI = new GoogleAI("AIzaSyAbgVsvTEuhdmn5UFSTMgHgXy-ijNdE0-I");
        var model = googleAI.GenerativeModel("gemini-2.5-flash");
        var response = await model.GenerateContent(prompt);

        return response.Text ?? string.Empty;
    }
}

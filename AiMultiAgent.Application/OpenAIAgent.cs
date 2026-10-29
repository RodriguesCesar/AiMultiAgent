using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenAI.Chat;

namespace AiMultiAgent.Application;

/// <summary>
/// Classe para integração com o LLM da OpenAI.
/// </summary>
public class OpenAIAgent(string apiKey, string model)
{
    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.Application");
    private static readonly Meter Meter = new("AiMultiAgent.Application");
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>("ai_agent_requests_total");
    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>("ai_agent_errors_total");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("ai_agent_duration_ms");

    private readonly string _apiKey = apiKey;
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4.1-mini" : model;

    public async Task<string> AskSync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenAI API Key não configurada. Use LLM__OpenAI__ApiKey.");

        var startedAt = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("llm.openai.request", ActivityKind.Client);
        activity?.SetTag("ai.provider", "openai");
        activity?.SetTag("ai.model", _model);

        RequestsCounter.Add(
            1,
            new KeyValuePair<string, object?>("provider", "openai"),
            new KeyValuePair<string, object?>("model", _model)
        );

        try
        {
            var client = new ChatClient(model: _model, apiKey: _apiKey);
            var response = await client.CompleteChatAsync(prompt);
            return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
        }
        catch
        {
            ErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>("provider", "openai"),
                new KeyValuePair<string, object?>("model", _model)
            );
            throw;
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            DurationMs.Record(
                elapsedMs,
                new KeyValuePair<string, object?>("provider", "openai"),
                new KeyValuePair<string, object?>("model", _model)
            );
        }
    }
}

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Mscc.GenerativeAI;

namespace AiMultiAgent.Application;

/// <summary>
/// Classe para integração com o LLM da Gemini, da Google.
/// </summary>
public class GeminiAgent(string apiKey, string model)
{
    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.Application");
    private static readonly Meter Meter = new("AiMultiAgent.Application");
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>("ai_agent_requests_total");
    private static readonly Counter<long> ErrorsCounter = Meter.CreateCounter<long>("ai_agent_errors_total");
    private static readonly Histogram<double> DurationMs = Meter.CreateHistogram<double>("ai_agent_duration_ms");

    private readonly string _apiKey = apiKey;
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gemini-2.5-flash" : model;

    public async Task<string> AskSync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt não pode ser vazio.", nameof(prompt));

        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("Gemini API Key não configurada. Use LLM__Gemini__ApiKey.");

        var startedAt = Stopwatch.GetTimestamp();

        using var activity = ActivitySource.StartActivity("llm.gemini.request", ActivityKind.Client);
        activity?.SetTag("ai.provider", "gemini");
        activity?.SetTag("ai.model", _model);

        RequestsCounter.Add(
            1,
            new KeyValuePair<string, object?>("provider", "gemini"),
            new KeyValuePair<string, object?>("model", _model)
        );

        try
        {
            var googleAI = new GoogleAI(_apiKey);
            var llm = googleAI.GenerativeModel(_model);
            var response = await llm.GenerateContent(prompt);
            return response.Text ?? string.Empty;
        }
        catch
        {
            ErrorsCounter.Add(
                1,
                new KeyValuePair<string, object?>("provider", "gemini"),
                new KeyValuePair<string, object?>("model", _model)
            );
            throw;
        }
        finally
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            DurationMs.Record(
                elapsedMs,
                new KeyValuePair<string, object?>("provider", "gemini"),
                new KeyValuePair<string, object?>("model", _model)
            );
        }
    }
}

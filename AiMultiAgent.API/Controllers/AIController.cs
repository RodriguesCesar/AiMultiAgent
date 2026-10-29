using System.Diagnostics;
using System.Diagnostics.Metrics;
using AiMultiAgent.API.DTOs;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiMultiAgent.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class AIController(IRabbitPublisher rabbitPublisher) : ControllerBase
{
    private static readonly ActivitySource ActivitySource = new("AiMultiAgent.API");
    private static readonly Meter Meter = new("AiMultiAgent.API");
    private static readonly Counter<long> PromptRequestsCounter = Meter.CreateCounter<long>("ai_api_prompts_received_total");
    private static readonly Counter<long> PromptValidationErrorsCounter = Meter.CreateCounter<long>("ai_api_prompt_validation_errors_total");

    [HttpPost]
    public async Task<IActionResult> ProcessPrompt([FromBody] PromptDTO prompt)
    {
        using var activity = ActivitySource.StartActivity("api.ai.process_prompt", ActivityKind.Server);

        PromptRequestsCounter.Add(1);

        if (string.IsNullOrWhiteSpace(prompt.message))
        {
            PromptValidationErrorsCounter.Add(1);
            return BadRequest(new { message = "message é obrigatório." });
        }

        await rabbitPublisher.PublishAsync(prompt.message);

        activity?.SetTag("ai.queue", "ai_queue");
        return Accepted(new { message = "Prompt recebido para processamento assíncrono." });
    }
}

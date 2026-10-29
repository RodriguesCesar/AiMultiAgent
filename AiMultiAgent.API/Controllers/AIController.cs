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
    [HttpPost]
    public async Task<IActionResult> ProcessPrompt([FromBody] PromptDTO prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt.message))
            return BadRequest(new { message = "message é obrigatório." });

        await rabbitPublisher.PublishAsync(prompt.message);
        return Accepted(new { message = "Prompt recebido para processamento assíncrono." });
    }
}

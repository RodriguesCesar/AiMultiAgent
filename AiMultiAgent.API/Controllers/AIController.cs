using AiMultiAgent.API.DTOs;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using Microsoft.AspNetCore.Mvc;
namespace AiMultiAgent.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AIController(IRabbitPublisher rabbitPublisher) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> ProcessPrompt([FromBody] PromptDTO prompt)
    {
        await rabbitPublisher.PublishAsync(prompt.message);
        return Accepted(new { message = $"Prompt recebido: {prompt.message}" });
    }
}

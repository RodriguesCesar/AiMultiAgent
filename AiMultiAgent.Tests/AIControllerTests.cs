using AiMultiAgent.API.Controllers;
using AiMultiAgent.API.DTOs;
using AiMultiAgent.Infrastructure.Rabbit.Infaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AiMultiAgent.Tests;

public class AIControllerTests
{
    [Fact]
    public async Task ProcessPrompt_WhenMessageIsEmpty_ReturnsBadRequest()
    {
        var publisher = new Mock<IRabbitPublisher>(MockBehavior.Strict);
        var controller = new AIController(publisher.Object);

        var result = await controller.ProcessPrompt(new PromptDTO("  "));

        Assert.IsType<BadRequestObjectResult>(result);
        publisher.Verify(x => x.PublishAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPrompt_WhenMessageIsValid_PublishesAndReturnsAccepted()
    {
        var publisher = new Mock<IRabbitPublisher>(MockBehavior.Strict);
        publisher.Setup(x => x.PublishAsync("olá mundo")).Returns(Task.CompletedTask).Verifiable();
        var controller = new AIController(publisher.Object);

        var result = await controller.ProcessPrompt(new PromptDTO("olá mundo"));

        Assert.IsType<AcceptedResult>(result);
        publisher.Verify();
    }
}

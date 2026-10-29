using AiMultiAgent.Application;

namespace AiMultiAgent.Tests;

public class OrchestratorAgentRoutingTests
{
    [Theory]
    [InlineData("Escreva uma API em C#", OrchestratorAgent.OpenAiProvider)]
    [InlineData("Qual o melhor algoritmo para ordenação?", OrchestratorAgent.OpenAiProvider)]
    [InlineData("Explique o conceito de orientação a objetos", OrchestratorAgent.GeminiProvider)]
    [InlineData("Me fale sobre história da computação", OrchestratorAgent.GeminiProvider)]
    [InlineData("Mensagem genérica sem palavras-chave", OrchestratorAgent.GeminiProvider)]
    public void SelectProvider_RoutesExpectedProvider(string prompt, string expectedProvider)
    {
        var provider = OrchestratorAgent.SelectProvider(prompt);

        Assert.Equal(expectedProvider, provider);
    }

    [Fact]
    public void SelectProvider_WhenPromptIsEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => OrchestratorAgent.SelectProvider(" "));
    }

    [Fact]
    public void SelectProvider_WhenProgrammingAndExplanationKeywordsExist_PrioritizesOpenAi()
    {
        var prompt = "Explique como funciona uma API em C#";

        var provider = OrchestratorAgent.SelectProvider(prompt);

        Assert.Equal(OrchestratorAgent.OpenAiProvider, provider);
    }
}

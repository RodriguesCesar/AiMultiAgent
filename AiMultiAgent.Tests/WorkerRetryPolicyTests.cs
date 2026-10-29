using System.Text;
using AiMultiAgent.Worker;

namespace AiMultiAgent.Tests;

public class WorkerRetryPolicyTests
{
    [Theory]
    [InlineData(0, 3, true)]
    [InlineData(2, 3, true)]
    [InlineData(3, 3, false)]
    [InlineData(4, 3, false)]
    public void ShouldRetry_AppliesExpectedRule(int currentRetry, int maxRetry, bool expected)
    {
        var result = Worker.ShouldRetry(currentRetry, maxRetry);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ParseRetryCount_WhenHeaderMissing_ReturnsZero()
    {
        var retryCount = Worker.ParseRetryCount(null);

        Assert.Equal(0, retryCount);
    }

    [Fact]
    public void ParseRetryCount_WhenHeaderIsInt_ReturnsValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = 2 };

        var retryCount = Worker.ParseRetryCount(headers);

        Assert.Equal(2, retryCount);
    }

    [Fact]
    public void ParseRetryCount_WhenHeaderIsByteArray_ReturnsParsedValue()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = Encoding.UTF8.GetBytes("3") };

        var retryCount = Worker.ParseRetryCount(headers);

        Assert.Equal(3, retryCount);
    }

    [Fact]
    public void ParseRetryCount_WhenHeaderIsInvalid_ReturnsZero()
    {
        var headers = new Dictionary<string, object?> { ["x-retry-count"] = "invalid" };

        var retryCount = Worker.ParseRetryCount(headers);

        Assert.Equal(0, retryCount);
    }
}

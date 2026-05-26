using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Application.Tests.Observability;

public sealed class CorrelationIdPolicyTests
{
    [Theory]
    [InlineData("request_ABC-123")]
    [InlineData("a")]
    public void AcceptOrCreate_AcceptsSafeIncomingIdentifiers(string incoming)
    {
        Assert.Equal(incoming, CorrelationIdPolicy.AcceptOrCreate(incoming));
    }

    [Theory]
    [InlineData("")]
    [InlineData("contains spaces")]
    [InlineData("unsafe/value")]
    public void AcceptOrCreate_ReplacesUnsafeIdentifiers(string incoming)
    {
        var correlationId = CorrelationIdPolicy.AcceptOrCreate(incoming);

        Assert.NotEqual(incoming, correlationId);
        Assert.Equal(32, correlationId.Length);
        Assert.True(CorrelationIdPolicy.IsAccepted(correlationId));
    }

    [Fact]
    public void AcceptOrCreate_ReplacesIdentifierLongerThanStorageLimit()
    {
        var incoming = new string('a', CorrelationIdPolicy.MaximumLength + 1);

        var correlationId = CorrelationIdPolicy.AcceptOrCreate(incoming);

        Assert.NotEqual(incoming, correlationId);
        Assert.True(CorrelationIdPolicy.IsAccepted(correlationId));
    }
}

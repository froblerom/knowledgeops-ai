using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Application.Tests.Observability;

public sealed class OperationalContractsTests
{
    [Fact]
    public void AuditEventTypes_ContainsOnlySprint8EmittedEventTypes()
    {
        var values = typeof(AuditEventTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.GetRawConstantValue())
            .Cast<string>()
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "HealthDetailsViewed",
                "PermissionDenied",
                "UserLoginFailure",
                "UserLoginSuccess"
            ],
            values);
    }

    [Fact]
    public void ProviderFailureCategory_DefinesContractOnlyForApprovedProviderAreas()
    {
        Assert.Equal("AiGeneration", ProviderFailureCategory.AiGeneration);
        Assert.Equal("EmbeddingGeneration", ProviderFailureCategory.EmbeddingGeneration);
        Assert.Equal("Retrieval", ProviderFailureCategory.Retrieval);
    }
}

using KnowledgeOps.Application.Observability;

namespace KnowledgeOps.Application.Tests.Observability;

public sealed class OperationalContractsTests
{
    [Fact]
    public void AuditEventTypes_ContainsApprovedOperationalAndUserManagementEventTypes()
    {
        var values = typeof(AuditEventTypes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(field => field.GetRawConstantValue())
            .Cast<string>()
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "ChatAnswerGenerationCompleted",
                "ChatAnswerGenerationFailed",
                "ChatHistoryDenied",
                "ChatHistoryViewed",
                "ChatInteractionStarted",
                "ChatInteractionStored",
                "ChatInteractionViewed",
                "CitationMappingFailed",
                "CitationsPersisted",
                "DocumentProcessingFailed",
                "DocumentProcessingStarted",
                "DocumentProcessingSucceeded",
                "DocumentRetrievalDisabled",
                "DocumentUploadAccepted",
                "DocumentUploadFailed",
                "DocumentUploadRejected",
                "EligibleSemanticRetrievalCompleted",
                "EligibleSemanticRetrievalFailed",
                "EmbeddingGenerationFailed",
                "EmbeddingGenerationSucceeded",
                "HealthDetailsViewed",
                "InsufficientContextReturned",
                "MalformedVectorExcluded",
                "PermissionDenied",
                "PromptBuildFailed",
                "QueryEmbeddingFailed",
                "RetrievalInsufficientResults",
                "SemanticQueryCompleted",
                "SemanticQueryFailed",
                "StaleRetrievalCandidateExcluded",
                "UserCreated",
                "UserLoginFailure",
                "UserLoginSuccess",
                "UserManagementDenied",
                "UserRoleAssigned",
                "UserRoleRemoved",
                "UserStatusChanged",
                "UserUpdated",
                "VectorIndexingFailed",
                "VectorIndexingSucceeded"
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

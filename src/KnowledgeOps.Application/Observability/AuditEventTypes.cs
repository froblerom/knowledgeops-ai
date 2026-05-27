namespace KnowledgeOps.Application.Observability;

public static class AuditEventTypes
{
    public const string UserLoginSuccess = "UserLoginSuccess";
    public const string UserLoginFailure = "UserLoginFailure";
    public const string PermissionDenied = "PermissionDenied";
    public const string HealthDetailsViewed = "HealthDetailsViewed";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string UserRoleAssigned = "UserRoleAssigned";
    public const string UserRoleRemoved = "UserRoleRemoved";
    public const string UserManagementDenied = "UserManagementDenied";
    public const string DocumentRetrievalDisabled = "DocumentRetrievalDisabled";
    public const string DocumentUploadAccepted = "DocumentUploadAccepted";
    public const string DocumentUploadRejected = "DocumentUploadRejected";
    public const string DocumentUploadFailed = "DocumentUploadFailed";
    public const string DocumentProcessingStarted = "DocumentProcessingStarted";
    public const string DocumentProcessingSucceeded = "DocumentProcessingSucceeded";
    public const string DocumentProcessingFailed = "DocumentProcessingFailed";
    public const string EmbeddingGenerationSucceeded = "EmbeddingGenerationSucceeded";
    public const string EmbeddingGenerationFailed = "EmbeddingGenerationFailed";
    public const string VectorIndexingSucceeded = "VectorIndexingSucceeded";
    public const string VectorIndexingFailed = "VectorIndexingFailed";
    public const string SemanticQueryCompleted = "SemanticQueryCompleted";
    public const string SemanticQueryFailed = "SemanticQueryFailed";
    public const string StaleRetrievalCandidateExcluded = "StaleRetrievalCandidateExcluded";
    public const string MalformedVectorExcluded = "MalformedVectorExcluded";
}

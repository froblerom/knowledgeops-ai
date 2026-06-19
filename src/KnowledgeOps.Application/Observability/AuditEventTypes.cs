namespace KnowledgeOps.Application.Observability;

public static class AuditEventTypes
{
    public const string UserLoginSuccess = "UserLoginSuccess";
    public const string UserLoginFailure = "UserLoginFailure";
    public const string PermissionDenied = "PermissionDenied";
    public const string HealthDetailsViewed = "HealthDetailsViewed";
    public const string AuditLogViewed = "AuditLogViewed";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string UserRoleAssigned = "UserRoleAssigned";
    public const string UserRoleRemoved = "UserRoleRemoved";
    public const string UserManagementDenied = "UserManagementDenied";
    public const string DocumentRetrievalDisabled = "DocumentRetrievalDisabled";
    public const string DocumentRetrievalEnabled = "DocumentRetrievalEnabled";
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
    public const string QueryEmbeddingFailed = "QueryEmbeddingFailed";
    public const string EligibleSemanticRetrievalCompleted = "EligibleSemanticRetrievalCompleted";
    public const string EligibleSemanticRetrievalFailed = "EligibleSemanticRetrievalFailed";
    public const string RetrievalInsufficientResults = "RetrievalInsufficientResults";
    public const string StaleRetrievalCandidateExcluded = "StaleRetrievalCandidateExcluded";
    public const string MalformedVectorExcluded = "MalformedVectorExcluded";
    public const string ChatInteractionStarted = "ChatInteractionStarted";
    public const string ChatAnswerGenerationCompleted = "ChatAnswerGenerationCompleted";
    public const string ChatAnswerGenerationFailed = "ChatAnswerGenerationFailed";
    public const string ChatInteractionStored = "ChatInteractionStored";
    public const string InsufficientContextReturned = "InsufficientContextReturned";
    public const string PromptBuildFailed = "PromptBuildFailed";
    public const string CitationsPersisted = "CitationsPersisted";
    public const string CitationMappingFailed = "CitationMappingFailed";
    public const string AnswerFeedbackSubmitted = "AnswerFeedbackSubmitted";
    public const string AnswerFeedbackUpdated = "AnswerFeedbackUpdated";
    public const string ChatHistoryViewed = "ChatHistoryViewed";
    public const string ChatInteractionViewed = "ChatInteractionViewed";
    public const string ChatHistoryDenied = "ChatHistoryDenied";
}

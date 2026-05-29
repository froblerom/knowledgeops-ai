using KnowledgeOps.Application.Chat;

namespace KnowledgeOps.Application.Chat.Prompting;

public sealed record GroundedPromptBuildRequest(
    string UserQuestion,
    Guid OrganizationId,
    IReadOnlyList<AuthorizedChunkContext> AuthorizedChunks);

public sealed record GroundedPromptBuildResult(
    bool IsSuccess,
    GroundedPrompt? GroundedPrompt,
    string? FailureCode,
    int IncludedChunkCount,
    int ExcludedChunkCount);

public sealed record GroundedPrompt(
    string PromptVersion,
    string SystemInstruction,
    string UserQuestion,
    string FormattedContext,
    IReadOnlyList<PromptSourceHandle> SourceHandles,
    IReadOnlyList<AuthorizedChunkContext> AuthorizedChunksForGeneration);

public sealed record PromptSourceHandle(
    Guid ChunkId,
    Guid DocumentId,
    int Rank,
    int? PageNumber,
    string? SectionLabel);

using System.Text;
using KnowledgeOps.Application.Authorization.Hooks;
using KnowledgeOps.Application.Chat;

namespace KnowledgeOps.Application.Chat.Prompting;

internal sealed class GroundedPromptBuilder(IPromptAuthorizationFilter authorizationFilter) : IGroundedPromptBuilder
{
    private const string PromptVersion = "rag-grounded-v1";
    private const int MaxContextChunks = 5;
    private const int MaxContextCharacters = 6000;

    private const string SystemInstruction =
        """
        You are a knowledge assistant. Answer questions using ONLY the provided approved document context below.

        Rules:
        - Base your answer solely on the context provided. Do not use external knowledge or make assumptions.
        - Do not invent, fabricate, or speculate about policy, HR guidance, legal, compliance, business, or operational instructions.
        - If the provided context does not contain enough information to answer the question safely, say so explicitly.
        - Acknowledge uncertainty when the context is unclear or incomplete.
        - Human supervisors and knowledge administrators remain the final authority on official guidance.

        If you cannot answer from the provided context, respond with:
        "I could not find sufficient information in the knowledge base to answer your question."
        """;

    public GroundedPromptBuildResult Build(GroundedPromptBuildRequest request)
    {
        var totalInput = request.AuthorizedChunks.Count;

        // Apply authorization filter: exclude chunks that don't pass org-scope check
        var filteredChunks = request.AuthorizedChunks
            .Where(chunk => authorizationFilter.IsChunkAuthorizedForPrompt(chunk.OrganizationId, request.OrganizationId))
            .ToList();

        // Apply MaxContextChunks limit
        var chunksAfterCountLimit = filteredChunks.Take(MaxContextChunks).ToList();

        // Apply MaxContextCharacters limit (keep at least one chunk)
        var includedChunks = new List<AuthorizedChunkContext>();
        var accumulatedChars = 0;

        foreach (var chunk in chunksAfterCountLimit)
        {
            if (includedChunks.Count == 0)
            {
                // Always include at least one chunk
                includedChunks.Add(chunk);
                accumulatedChars += chunk.ChunkText.Length;
            }
            else if (accumulatedChars + chunk.ChunkText.Length <= MaxContextCharacters)
            {
                includedChunks.Add(chunk);
                accumulatedChars += chunk.ChunkText.Length;
            }
            else
            {
                break;
            }
        }

        var excludedChunkCount = totalInput - includedChunks.Count;

        if (includedChunks.Count == 0)
        {
            return new GroundedPromptBuildResult(
                IsSuccess: false,
                GroundedPrompt: null,
                FailureCode: "NoAuthorizedChunks",
                IncludedChunkCount: 0,
                ExcludedChunkCount: totalInput);
        }

        // Format context
        var contextBuilder = new StringBuilder();
        var sourceHandles = new List<PromptSourceHandle>();

        for (var i = 0; i < includedChunks.Count; i++)
        {
            var chunk = includedChunks[i];
            var rank = i + 1;
            var label = chunk.SectionLabel ?? $"Chunk {chunk.ChunkIndex}";
            var page = chunk.PageNumber?.ToString() ?? "N/A";

            contextBuilder.Append($"[{rank}] {label} (Page {page})\n{chunk.ChunkText}\n\n");

            sourceHandles.Add(new PromptSourceHandle(
                ChunkId: chunk.ChunkId,
                DocumentId: chunk.DocumentId,
                Rank: rank,
                PageNumber: chunk.PageNumber,
                SectionLabel: chunk.SectionLabel));
        }

        var groundedPrompt = new GroundedPrompt(
            PromptVersion: PromptVersion,
            SystemInstruction: SystemInstruction,
            UserQuestion: request.UserQuestion,
            FormattedContext: contextBuilder.ToString(),
            SourceHandles: sourceHandles,
            AuthorizedChunksForGeneration: includedChunks);

        return new GroundedPromptBuildResult(
            IsSuccess: true,
            GroundedPrompt: groundedPrompt,
            FailureCode: null,
            IncludedChunkCount: includedChunks.Count,
            ExcludedChunkCount: excludedChunkCount);
    }
}

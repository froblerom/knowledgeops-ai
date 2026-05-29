using KnowledgeOps.Domain.Chat;

namespace KnowledgeOps.Application.Chat.Citations;

public interface ICitationRepository
{
    Task AddRangeAsync(IReadOnlyList<Citation> citations, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

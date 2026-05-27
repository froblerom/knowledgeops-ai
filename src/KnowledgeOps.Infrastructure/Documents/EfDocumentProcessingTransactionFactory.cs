using KnowledgeOps.Application.Documents;
using KnowledgeOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class EfDocumentProcessingTransactionFactory(KnowledgeOpsDbContext dbContext)
    : IDocumentProcessingTransactionFactory
{
    public async Task<IDocumentProcessingTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfDocumentProcessingTransaction(tx);
    }

    private sealed class EfDocumentProcessingTransaction(IDbContextTransaction transaction)
        : IDocumentProcessingTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}

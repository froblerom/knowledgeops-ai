namespace KnowledgeOps.Application.Documents;

public interface IDocumentProcessingTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IDocumentProcessingTransactionFactory
{
    Task<IDocumentProcessingTransaction> BeginAsync(CancellationToken cancellationToken = default);
}

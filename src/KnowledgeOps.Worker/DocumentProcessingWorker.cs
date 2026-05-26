using KnowledgeOps.Application.Documents;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Worker;

internal sealed class DocumentProcessingWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<WorkerSettings> settings,
    ILogger<DocumentProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = settings.Value.EffectivePollingIntervalSeconds;

        logger.LogInformation(
            "Document processing worker started. PollingIntervalSeconds={PollingIntervalSeconds}",
            intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOneCycleAsync(stoppingToken);
        }

        logger.LogInformation("Document processing worker stopped.");
    }

    private async Task ProcessOneCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<IDocumentProcessingOrchestrator>();

            var processed = await orchestrator.ProcessNextAsync(stoppingToken);

            if (processed)
            {
                logger.LogDebug("Document processing cycle completed with work.");
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown — do not log as error.
        }
        catch (Exception ex)
        {
            // Log safe summary without stack trace, paths, or document content.
            logger.LogError(
                "Document processing cycle encountered an unhandled error: {Message}",
                SafeMessage(ex));
        }
    }

    private static string SafeMessage(Exception ex) =>
        ex.Message?.Trim() is { Length: > 0 } msg
            ? msg.Length <= 200 ? msg : msg[..200]
            : "Unexpected processing error.";
}

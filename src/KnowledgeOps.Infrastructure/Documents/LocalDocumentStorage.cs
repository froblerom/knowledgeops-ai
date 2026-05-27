using KnowledgeOps.Application.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.Infrastructure.Documents;

internal sealed class LocalDocumentStorage(
    IOptions<LocalStorageSettings> options,
    ILogger<LocalDocumentStorage> logger) : IDocumentStorage
{
    private const string LocalScheme = "local://";

    public async Task<StoredDocumentReference> StoreAsync(
        Stream fileStream,
        string safeFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var root = ResolveRoot();
        Directory.CreateDirectory(root);

        var storedName = $"{Guid.NewGuid():N}_{Path.GetFileName(safeFileName)}";
        var fullPath = Path.Combine(root, storedName);

        await using var fileOutput = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await fileStream.CopyToAsync(fileOutput, cancellationToken);

        logger.LogDebug(
            "Document stored. StoredName={StoredName}",
            storedName);

        return new StoredDocumentReference($"{LocalScheme}{storedName}");
    }

    public Task<Stream> OpenReadAsync(string storageReference, CancellationToken cancellationToken = default)
    {
        if (!storageReference.StartsWith(LocalScheme, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Unrecognized storage reference scheme.");

        var storedName = storageReference[LocalScheme.Length..];
        if (string.IsNullOrWhiteSpace(storedName))
            throw new InvalidOperationException("Storage reference contains an empty stored name.");

        var root = ResolveRoot();
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        var fullPath = Path.GetFullPath(Path.Combine(root, storedName));
        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Resolved path is outside the configured storage root.");

        logger.LogDebug("Opening document for read. StoredName={StoredName}", storedName);

        try
        {
            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
            return Task.FromResult(stream);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException
            or UnauthorizedAccessException or IOException)
        {
            throw new InvalidOperationException("Document file could not be opened for reading.");
        }
    }

    public Task DeleteAsync(string storageReference, CancellationToken cancellationToken = default)
    {
        if (!storageReference.StartsWith(LocalScheme, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "DeleteAsync called with unrecognized storage reference scheme. Reference skipped.");
            return Task.CompletedTask;
        }

        var storedName = storageReference[LocalScheme.Length..];
        if (string.IsNullOrWhiteSpace(storedName))
        {
            logger.LogWarning("DeleteAsync called with empty stored name. Reference skipped.");
            return Task.CompletedTask;
        }

        var root = ResolveRoot();

        // Ensure root ends with a separator so a prefix like /storage/documents-evil
        // cannot fool the StartsWith containment check.
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        var fullPath = Path.GetFullPath(Path.Combine(root, storedName));

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "DeleteAsync blocked: resolved path is outside configured storage root. Reference skipped.");
            return Task.CompletedTask;
        }

        try
        {
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Best-effort delete of stored file failed. StoredName={StoredName}",
                storedName);
        }

        return Task.CompletedTask;
    }

    private string ResolveRoot()
    {
        var configured = options.Value.LocalDocumentsPath;
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException(
                "Local document storage path is not configured. " +
                "Set 'Storage:LocalDocumentsPath' in appsettings.Development.json or via environment variable.");

        return Path.GetFullPath(configured);
    }
}

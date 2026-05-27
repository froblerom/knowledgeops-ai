using KnowledgeOps.Infrastructure.Documents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KnowledgeOps.IntegrationTests;

public sealed class LocalDocumentStorageTests
{
    [Fact]
    public async Task OpenReadAsync_MissingFile_ThrowsSafeExceptionWithoutAbsolutePath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var settings = Options.Create(new LocalStorageSettings { LocalDocumentsPath = tempDir });
            var storage = new LocalDocumentStorage(settings, NullLogger<LocalDocumentStorage>.Instance);
            var storageRef = $"local://{Guid.NewGuid():N}_missing.txt";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => storage.OpenReadAsync(storageRef));

            Assert.Equal("Document file could not be opened for reading.", ex.Message);
            Assert.DoesNotContain(tempDir, ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(":\\", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("local://", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}

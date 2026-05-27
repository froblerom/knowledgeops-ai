using System.Text.Json;

namespace KnowledgeOps.IntegrationTests;

public sealed class RetrievalConfigurationTests
{
    [Fact]
    public void RetrievalProviderConfig_DoesNotRequireSecrets()
    {
        AssertRetrievalSectionIsSafe(
            Path.Combine("src", "KnowledgeOps.Api", "appsettings.json"));
        AssertRetrievalSectionIsSafe(
            Path.Combine("src", "KnowledgeOps.Worker", "appsettings.json"));
    }

    private static void AssertRetrievalSectionIsSafe(string relativePath)
    {
        var configPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", relativePath));
        using var document = JsonDocument.Parse(File.ReadAllText(configPath));

        var retrieval = document.RootElement.GetProperty("Retrieval");
        var retrievalJson = retrieval.GetRawText();
        var propertyNames = retrieval.EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["DefaultTopK", "MaxTopK"], propertyNames);
        Assert.DoesNotContain("key", retrievalJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", retrievalJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("endpoint", retrievalJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection", retrievalJson, StringComparison.OrdinalIgnoreCase);
    }
}

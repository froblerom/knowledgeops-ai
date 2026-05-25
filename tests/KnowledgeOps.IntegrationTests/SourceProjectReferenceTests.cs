using System.Xml.Linq;

namespace KnowledgeOps.IntegrationTests;

public sealed class SourceProjectReferenceTests
{
    [Fact]
    public void Infrastructure_Should_Reference_Application_And_Domain()
    {
        AssertProjectReferences(
            "KnowledgeOps.Infrastructure",
            "KnowledgeOps.Application",
            "KnowledgeOps.Domain");
    }

    [Fact]
    public void Api_Should_Reference_Application_And_Infrastructure()
    {
        AssertProjectReferences(
            "KnowledgeOps.Api",
            "KnowledgeOps.Application",
            "KnowledgeOps.Infrastructure");
    }

    [Fact]
    public void Worker_Should_Reference_Application_And_Infrastructure()
    {
        AssertProjectReferences(
            "KnowledgeOps.Worker",
            "KnowledgeOps.Application",
            "KnowledgeOps.Infrastructure");
    }

    [Theory]
    [InlineData("KnowledgeOps.Domain")]
    [InlineData("KnowledgeOps.Application")]
    public void Inner_Layers_Should_Not_Reference_EntityFrameworkCore_Packages(string projectName)
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            projectName,
            $"{projectName}.csproj");

        var packageReferences = XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty);

        Assert.DoesNotContain(
            packageReferences,
            packageName => packageName.StartsWith(
                "Microsoft.EntityFrameworkCore",
                StringComparison.Ordinal));
    }

    private static void AssertProjectReferences(string projectName, params string[] expectedReferences)
    {
        var projectPath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            projectName,
            $"{projectName}.csproj");

        var actualReferences = XDocument.Load(projectPath)
            .Descendants()
            .Where(element => element.Name.LocalName == "ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Select(reference => NormalizeReferencedProjectName(reference!))
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        var orderedExpectedReferences = expectedReferences
            .OrderBy(reference => reference, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(orderedExpectedReferences, actualReferences);
    }

    private static string NormalizeReferencedProjectName(string projectReference)
    {
        var normalizedReference = projectReference
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        return Path.GetFileNameWithoutExtension(normalizedReference);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KnowledgeOpsAI.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate KnowledgeOpsAI.sln above {AppContext.BaseDirectory}.");
    }
}

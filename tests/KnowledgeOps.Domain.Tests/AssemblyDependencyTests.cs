using System.Reflection;

namespace KnowledgeOps.Domain.Tests;

public sealed class AssemblyDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(KnowledgeOps.Domain.AssemblyMarker).Assembly;

    [Fact]
    public void Domain_Should_Not_Reference_Api()
    {
        AssertDoesNotReference("KnowledgeOps.Api");
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        AssertDoesNotReference("KnowledgeOps.Infrastructure");
    }

    [Fact]
    public void Domain_Should_Not_Reference_EntityFrameworkCore()
    {
        AssertDoesNotReference("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Domain_Should_Not_Reference_AspNetCore()
    {
        AssertDoesNotReference("Microsoft.AspNetCore");
    }

    [Theory]
    [InlineData("Azure.AI.OpenAI")]
    [InlineData("OpenAI")]
    [InlineData("Azure.Storage.Blobs")]
    [InlineData("Azure.Search.Documents")]
    [InlineData("Microsoft.SemanticKernel")]
    [InlineData("Qdrant")]
    [InlineData("Pinecone")]
    [InlineData("Chroma")]
    [InlineData("Weaviate")]
    [InlineData("Milvus")]
    public void Domain_Should_Not_Reference_AiProviderSdks(string forbiddenAssemblyPrefix)
    {
        AssertDoesNotReference(forbiddenAssemblyPrefix);
    }

    private static void AssertDoesNotReference(string forbiddenAssemblyPrefix)
    {
        var referencedAssemblyNames = DomainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty);

        Assert.DoesNotContain(
            referencedAssemblyNames,
            assemblyName => IsAssemblyOrChild(assemblyName, forbiddenAssemblyPrefix));
    }

    private static bool IsAssemblyOrChild(string assemblyName, string prefix)
    {
        return string.Equals(assemblyName, prefix, StringComparison.Ordinal)
            || assemblyName.StartsWith($"{prefix}.", StringComparison.Ordinal);
    }
}

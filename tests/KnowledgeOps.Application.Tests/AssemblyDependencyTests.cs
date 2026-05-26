using System.Reflection;

namespace KnowledgeOps.Application.Tests;

public sealed class AssemblyDependencyTests
{
    private static readonly Assembly ApplicationAssembly = typeof(KnowledgeOps.Application.AssemblyMarker).Assembly;

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        AssertDoesNotReference("KnowledgeOps.Infrastructure");
    }

    [Fact]
    public void Application_Should_Not_Reference_EntityFrameworkCore()
    {
        AssertDoesNotReference("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Application_Should_Not_Reference_AspNetCoreAuthentication()
    {
        AssertDoesNotReference("Microsoft.AspNetCore.Authentication");
    }

    [Fact]
    public void Application_Should_Not_Reference_SystemIdentityModelTokensJwt()
    {
        AssertDoesNotReference("System.IdentityModel.Tokens.Jwt");
    }

    [Fact]
    public void Application_Should_Not_Reference_MicrosoftIdentityModelTokens()
    {
        AssertDoesNotReference("Microsoft.IdentityModel.Tokens");
    }

    [Theory]
    [InlineData("Azure.AI.OpenAI")]
    [InlineData("OpenAI")]
    [InlineData("Azure.Storage.Blobs")]
    [InlineData("Azure.Search.Documents")]
    public void Application_Should_Not_Reference_AiProviderSdks(string forbiddenAssemblyPrefix)
    {
        AssertDoesNotReference(forbiddenAssemblyPrefix);
    }

    private static void AssertDoesNotReference(string forbiddenAssemblyPrefix)
    {
        var referencedAssemblyNames = ApplicationAssembly.GetReferencedAssemblies()
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

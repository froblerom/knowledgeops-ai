using System.Reflection;
using KnowledgeOps.Application.Auth.Abstractions;
using KnowledgeOps.Application.Authorization;
using KnowledgeOps.Application.Documents;

namespace KnowledgeOps.Application.Tests.DI;

/// <summary>
/// Guards the boundary between AddApplicationCore() (Worker-safe) and
/// AddApplicationApiFeatures() (API-only). Every concrete type registered in
/// AddApplicationCore() must be constructible without ICurrentUser, because the
/// Worker host has no HTTP request context.
/// </summary>
public sealed class WorkerDiCompositionTests
{
    public static IEnumerable<object[]> CoreConcreteTypes =>
    [
        [typeof(DocumentProcessingOrchestrator)],
        [typeof(ExtractAndChunkDocumentProcessingStep)],
        [typeof(GenerateChunkEmbeddingsProcessingStep)],
        [typeof(PermissionService)],
        [typeof(OrganizationScopeService)]
    ];

    [Theory]
    [MemberData(nameof(CoreConcreteTypes))]
    public void AddApplicationCore_ConcreteType_DoesNotDependOnICurrentUser(Type type)
    {
        var constructors = type.GetConstructors(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var ctor in constructors)
        {
            var hasICurrentUser = ctor.GetParameters()
                .Any(p => p.ParameterType == typeof(ICurrentUser));

            Assert.False(hasICurrentUser,
                $"{type.FullName} must not inject ICurrentUser. " +
                $"It is registered in AddApplicationCore(), which runs in the Worker host " +
                $"where no HTTP request context exists.");
        }
    }
}

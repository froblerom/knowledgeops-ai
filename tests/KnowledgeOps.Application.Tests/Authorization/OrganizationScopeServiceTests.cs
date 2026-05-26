using KnowledgeOps.Application.Authorization;

namespace KnowledgeOps.Application.Tests.Authorization;

public sealed class OrganizationScopeServiceTests
{
    private static readonly IOrganizationScopeService Service = new OrganizationScopeService();

    private static readonly Guid OrgA = new("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OrgB = new("22222222-2222-4222-8222-222222222222");

    [Fact]
    public void CheckScope_SameOrganization_ReturnsAllowed()
    {
        var result = Service.CheckScope(OrgA, OrgA);
        Assert.True(result.IsAllowed);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void CheckScope_DifferentOrganizations_ReturnsDeniedWithCrossOrgReason()
    {
        var result = Service.CheckScope(OrgA, OrgB);
        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.CrossOrganizationAccess, result.FailureReason);
    }

    [Fact]
    public void CheckScope_CurrentUserGuidEmpty_ReturnsMissingOrganization()
    {
        var result = Service.CheckScope(Guid.Empty, OrgA);
        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.MissingOrganization, result.FailureReason);
    }

    [Fact]
    public void CheckScope_TargetGuidEmpty_ReturnsMissingTargetOrganization()
    {
        var result = Service.CheckScope(OrgA, Guid.Empty);
        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.MissingTargetOrganization, result.FailureReason);
    }

    [Fact]
    public void CheckScope_BothGuidEmpty_ReturnsMissingOrganization()
    {
        // Current-user Guid.Empty is checked first — missing current org takes priority.
        var result = Service.CheckScope(Guid.Empty, Guid.Empty);
        Assert.False(result.IsAllowed);
        Assert.Equal(AuthorizationFailureReason.MissingOrganization, result.FailureReason);
    }

    [Fact]
    public void IsInScope_SameOrganization_ReturnsTrue()
    {
        Assert.True(Service.IsInScope(OrgA, OrgA));
    }

    [Fact]
    public void IsInScope_DifferentOrganizations_ReturnsFalse()
    {
        Assert.False(Service.IsInScope(OrgA, OrgB));
    }

    [Fact]
    public void IsInScope_GuidEmpty_ReturnsFalse()
    {
        Assert.False(Service.IsInScope(Guid.Empty, OrgA));
        Assert.False(Service.IsInScope(OrgA, Guid.Empty));
    }

    [Fact]
    public void AdminRole_DoesNotBypassCrossOrganizationCheck()
    {
        // Admin is organization-scoped for MVP (ADR-010). Same rules apply.
        var result = Service.CheckScope(OrgA, OrgB);
        Assert.False(result.IsAllowed);
    }
}

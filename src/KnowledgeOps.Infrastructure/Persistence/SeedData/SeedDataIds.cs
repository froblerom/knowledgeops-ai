namespace KnowledgeOps.Infrastructure.Persistence.SeedData;

public static class SeedDataIds
{
    // Organizations
    public static readonly Guid AsteriaOrganizationId =
        new("11111111-1111-4111-8111-111111111111");

    public static readonly Guid BorealOrganizationId =
        new("22222222-2222-4222-8222-222222222222");

    // Asteria Support Group users
    public static readonly Guid AsteriaAgentUserId =
        new("aaaa0001-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public static readonly Guid AsteriaSupervisorUserId =
        new("aaaa0002-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public static readonly Guid AsteriaKnowledgeAdminUserId =
        new("aaaa0003-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public static readonly Guid AsteriaManagerUserId =
        new("aaaa0004-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    public static readonly Guid AsteriaAdminUserId =
        new("aaaa0005-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    // Boreal Contact Services users
    public static readonly Guid BorealAgentUserId =
        new("bbbb0001-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    public static readonly Guid BorealAdminUserId =
        new("bbbb0002-bbbb-4bbb-8bbb-bbbbbbbbbbbb");

    // User role assignment IDs (surrogate PK required by user_roles table)
    public static readonly Guid AsteriaAgentRoleId =
        new("cccc0001-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid AsteriaSupervisorRoleId =
        new("cccc0002-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid AsteriaKnowledgeAdminRoleId =
        new("cccc0003-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid AsteriaManagerRoleId =
        new("cccc0004-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid AsteriaAdminRoleId =
        new("cccc0005-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid BorealAgentRoleId =
        new("cccc0006-cccc-4ccc-8ccc-cccccccccccc");

    public static readonly Guid BorealAdminRoleId =
        new("cccc0007-cccc-4ccc-8ccc-cccccccccccc");
}

using KnowledgeOps.Domain.Organizations;
using KnowledgeOps.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeOps.Infrastructure.Persistence.SeedData;

public static class KnowledgeOpsSeedData
{
    private static readonly DateTimeOffset SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void ApplySeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>().HasData(
            new Organization
            {
                Id = SeedDataIds.AsteriaOrganizationId,
                Name = "Asteria Support Group",
                Status = OrganizationStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new Organization
            {
                Id = SeedDataIds.BorealOrganizationId,
                Name = "Boreal Contact Services",
                Status = OrganizationStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = SeedDataIds.AsteriaAgentUserId,
                OrganizationId = SeedDataIds.AsteriaOrganizationId,
                DisplayName = "Agent A",
                Email = "agent.a@asteria.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.AsteriaSupervisorUserId,
                OrganizationId = SeedDataIds.AsteriaOrganizationId,
                DisplayName = "Supervisor A",
                Email = "supervisor.a@asteria.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.AsteriaKnowledgeAdminUserId,
                OrganizationId = SeedDataIds.AsteriaOrganizationId,
                DisplayName = "KnowledgeAdmin A",
                Email = "knowledgeadmin.a@asteria.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.AsteriaManagerUserId,
                OrganizationId = SeedDataIds.AsteriaOrganizationId,
                DisplayName = "Manager A",
                Email = "manager.a@asteria.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.AsteriaAdminUserId,
                OrganizationId = SeedDataIds.AsteriaOrganizationId,
                DisplayName = "Admin A",
                Email = "admin.a@asteria.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.BorealAgentUserId,
                OrganizationId = SeedDataIds.BorealOrganizationId,
                DisplayName = "Agent B",
                Email = "agent.b@boreal.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            },
            new User
            {
                Id = SeedDataIds.BorealAdminUserId,
                OrganizationId = SeedDataIds.BorealOrganizationId,
                DisplayName = "Admin B",
                Email = "admin.b@boreal.example.com",
                PasswordHash = null,
                Status = UserStatus.Active,
                CreatedAt = SeedTimestamp,
                UpdatedAt = SeedTimestamp
            });

        modelBuilder.Entity<UserRole>().HasData(
            new UserRole
            {
                Id = SeedDataIds.AsteriaAgentRoleId,
                UserId = SeedDataIds.AsteriaAgentUserId,
                RoleName = UserRoleName.Agent,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.AsteriaSupervisorRoleId,
                UserId = SeedDataIds.AsteriaSupervisorUserId,
                RoleName = UserRoleName.Supervisor,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.AsteriaKnowledgeAdminRoleId,
                UserId = SeedDataIds.AsteriaKnowledgeAdminUserId,
                RoleName = UserRoleName.KnowledgeAdmin,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.AsteriaManagerRoleId,
                UserId = SeedDataIds.AsteriaManagerUserId,
                RoleName = UserRoleName.Manager,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.AsteriaAdminRoleId,
                UserId = SeedDataIds.AsteriaAdminUserId,
                RoleName = UserRoleName.Admin,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.BorealAgentRoleId,
                UserId = SeedDataIds.BorealAgentUserId,
                RoleName = UserRoleName.Agent,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            },
            new UserRole
            {
                Id = SeedDataIds.BorealAdminRoleId,
                UserId = SeedDataIds.BorealAdminUserId,
                RoleName = UserRoleName.Admin,
                AssignedAt = SeedTimestamp,
                AssignedByUserId = null
            });
    }
}

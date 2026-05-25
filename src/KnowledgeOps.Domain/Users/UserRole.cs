namespace KnowledgeOps.Domain.Users;

public sealed class UserRole
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserRoleName RoleName { get; set; }

    public DateTimeOffset AssignedAt { get; set; }

    public Guid? AssignedByUserId { get; set; }
}

using KVYS.Identity.Domain.Enums;

namespace KVYS.Identity.Domain.Entities;

/// <summary>
/// Junction table for role-permission relationship.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoleId { get; set; }
    public Permission Permission { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ApplicationRole Role { get; set; } = null!;
}

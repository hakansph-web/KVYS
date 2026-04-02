using KVYS.Identity.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace KVYS.Identity.Domain.Entities;

/// <summary>
/// Application role extending ASP.NET Core Identity.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public SystemRole? SystemRoleType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = [];
}

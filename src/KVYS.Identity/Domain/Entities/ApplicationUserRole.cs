using Microsoft.AspNetCore.Identity;

namespace KVYS.Identity.Domain.Entities;

/// <summary>
/// Junction table for user-role relationship with additional metadata.
/// </summary>
public class ApplicationUserRole : IdentityUserRole<Guid>
{
    public Guid? ScopeId { get; set; }
    public string? ScopeType { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public Guid? AssignedBy { get; set; }

    public virtual ApplicationUser User { get; set; } = null!;
    public virtual ApplicationRole Role { get; set; } = null!;
}

using Ate.Domain.Common;

namespace Ate.Domain.Entities;

public class User : BaseAuditableEntity
{
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public Guid? CompanyId { get; set; }

    public Company? Company { get; set; }
    public Person? Person { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();
}

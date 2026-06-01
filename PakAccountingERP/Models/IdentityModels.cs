using Microsoft.AspNetCore.Identity;
using PakAccountingERP.Models.Common;

namespace PakAccountingERP.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
}

public class Company : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FbrHttpPostUrl { get; set; }
    public string? ApiToken { get; set; }
    public string? LogoPath { get; set; }
    public bool IsDefault { get; set; }
    public ICollection<UserCompany> UserCompanies { get; set; } = new List<UserCompany>();
}

public class UserCompany : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public bool IsDefault { get; set; }
}

public class Province : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}

public class Permission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

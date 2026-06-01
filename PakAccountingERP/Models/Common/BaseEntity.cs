namespace PakAccountingERP.Models.Common;

/// <summary>
/// Base entity with audit fields and soft delete support.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

/// <summary>
/// Company-scoped entity base.
/// </summary>
public abstract class CompanyEntity : BaseEntity
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}

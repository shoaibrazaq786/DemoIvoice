using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Models;

namespace PakAccountingERP.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompany> UserCompanies => Set<UserCompany>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<TaxSetting> TaxSettings => Set<TaxSetting>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
    public DbSet<BankReconciliationItem> BankReconciliationItems => Set<BankReconciliationItem>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ApplySoftDeleteFilters(builder);
        ConfigureRelationships(builder);
        ConfigureDecimalPrecision(builder);
        ConfigureIndexes(builder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(Models.Common.BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { builder });
            }
        }
    }

    private static void SetSoftDeleteFilter<T>(ModelBuilder builder) where T : Models.Common.BaseEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureDecimalPrecision(ModelBuilder builder)
    {
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }
    }

    private static void ConfigureIndexes(ModelBuilder builder)
    {
        builder.Entity<Company>().HasIndex(c => c.CompanyName);
        builder.Entity<Customer>().HasIndex(c => new { c.CompanyId, c.BuyerId }).IsUnique();
        builder.Entity<Vendor>().HasIndex(v => new { v.CompanyId, v.VendorCode }).IsUnique();
        builder.Entity<Item>().HasIndex(i => new { i.CompanyId, i.ItemCode }).IsUnique();
        builder.Entity<SalesInvoice>().HasIndex(i => new { i.CompanyId, i.InvoiceNumber }).IsUnique();
        builder.Entity<Bill>().HasIndex(b => new { b.CompanyId, b.BillNumber }).IsUnique();
        builder.Entity<ChartOfAccount>().HasIndex(a => new { a.CompanyId, a.AccountNumber }).IsUnique();
        builder.Entity<AuditLog>().HasIndex(a => a.ActionDate);
    }

    private static void ConfigureRelationships(ModelBuilder builder)
    {
        // SQL Server rejects multiple cascade paths; default all FKs to Restrict.
        foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            relationship.DeleteBehavior = DeleteBehavior.Restrict;

        builder.Entity<UserCompany>()
            .HasOne(uc => uc.User).WithMany(u => u.UserCompanies).HasForeignKey(uc => uc.UserId);
        builder.Entity<UserCompany>()
            .HasOne(uc => uc.Company).WithMany(c => c.UserCompanies).HasForeignKey(uc => uc.CompanyId);

        builder.Entity<ChartOfAccount>()
            .HasOne(a => a.ParentAccount).WithMany(a => a.ChildAccounts)
            .HasForeignKey(a => a.ParentAccountId).OnDelete(DeleteBehavior.Restrict);

        // Cascade only for owned line-item collections (single delete path).
        builder.Entity<SalesInvoiceItem>()
            .HasOne(i => i.SalesInvoice).WithMany(i => i.Items)
            .HasForeignKey(i => i.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BillItem>()
            .HasOne(i => i.Bill).WithMany(b => b.Items)
            .HasForeignKey(i => i.BillId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<JournalEntryLine>()
            .HasOne(l => l.JournalEntry).WithMany(e => e.Lines)
            .HasForeignKey(l => l.JournalEntryId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BankReconciliationItem>()
            .HasOne(i => i.BankReconciliation).WithMany(r => r.Items)
            .HasForeignKey(i => i.BankReconciliationId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BankTransaction>()
            .HasOne(t => t.TransferToBank).WithMany()
            .HasForeignKey(t => t.TransferToBankId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<SalesInvoice>()
            .HasOne(i => i.Customer).WithMany(c => c.SalesInvoices)
            .HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Bill>()
            .HasOne(b => b.Vendor).WithMany(v => v.Bills)
            .HasForeignKey(b => b.VendorId).OnDelete(DeleteBehavior.Restrict);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Models.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}

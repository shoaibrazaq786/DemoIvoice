using PakAccountingERP.Models.Common;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.Models;

public class ChartOfAccount : CompanyEntity
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public int? ParentAccountId { get; set; }
    public ChartOfAccount? ParentAccount { get; set; }
    public AccountType AccountType { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ChartOfAccount> ChildAccounts { get; set; } = new List<ChartOfAccount>();
}

public class Customer : CompanyEntity
{
    public string BuyerId { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string? Address { get; set; }
    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? NTN { get; set; }
    public string? CNIC { get; set; }
    public string? STRN { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Registered;
    public SalesType SalesType { get; set; } = SalesType.StandardRate;
    public InvoiceTypeEnum InvoiceType { get; set; } = InvoiceTypeEnum.SalesInvoice;
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}

public class Vendor : CompanyEntity
{
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public string? Address { get; set; }
    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public decimal DefaultSalesTaxRate { get; set; } = 18m;
    public ICollection<Bill> Bills { get; set; } = new List<Bill>();
}

public class UnitOfMeasure : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
}

public class ItemCategory : CompanyEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Item> Items { get; set; } = new List<Item>();
}

public class Item : CompanyEntity
{
    public ItemType ItemType { get; set; } = ItemType.Inventory;
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HSCode { get; set; }
    public string? Barcode { get; set; }
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public int? ItemCategoryId { get; set; }
    public ItemCategory? ItemCategory { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public CostingMethod CostingMethod { get; set; } = CostingMethod.FIFO;
}

public class TaxSetting : CompanyEntity
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal SalesTaxRate { get; set; }
    public decimal UnregisteredSalesTaxRate { get; set; }
}

public class Warehouse : CompanyEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
}

public class InventoryTransaction : CompanyEntity
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? BatchExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal BalanceQuantity { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Notes { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}

public class InventoryBatch : CompanyEntity
{
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ReceivedDate { get; set; }
}

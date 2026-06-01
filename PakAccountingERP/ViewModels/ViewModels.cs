using System.ComponentModel.DataAnnotations;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class CompanyViewModel
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public int? ProvinceId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FbrHttpPostUrl { get; set; }
    public string? ApiToken { get; set; }
    public bool IsDefault { get; set; }
    public IFormFile? Logo { get; set; }
}

public class CustomerViewModel
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string BuyerName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public string? Address { get; set; }
    public int? ProvinceId { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public string? NTN { get; set; }
    public string? CNIC { get; set; }
    public string? STRN { get; set; }
    public CustomerType CustomerType { get; set; }
    public SalesType SalesType { get; set; }
    public InvoiceTypeEnum InvoiceType { get; set; }
}

public class VendorViewModel
{
    public int Id { get; set; }
    public string VendorCode { get; set; } = string.Empty;

    [Required]
    public string VendorName { get; set; } = string.Empty;

    public decimal OpeningBalance { get; set; }
    public string? Address { get; set; }
    public int? ProvinceId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NTN { get; set; }
    public string? STRN { get; set; }
    public decimal DefaultSalesTaxRate { get; set; } = 18m;
}

public class ItemViewModel
{
    public int Id { get; set; }
    public ItemType ItemType { get; set; }

    [Required]
    public string ItemCode { get; set; } = string.Empty;

    [Required]
    public string ItemName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public string? HSCode { get; set; }
    public string? Barcode { get; set; }
    public int UnitOfMeasureId { get; set; }
    public int? ItemCategoryId { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal SaleRate { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public CostingMethod CostingMethod { get; set; }
}

public class ChartOfAccountViewModel
{
    public int Id { get; set; }

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public string AccountName { get; set; } = string.Empty;

    public int? ParentAccountId { get; set; }
    public AccountType AccountType { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ChartOfAccountViewModel> Children { get; set; } = new();
}

public class InvoiceLineViewModel
{
    public int ItemId { get; set; }
    public string? HSCode { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public int UnitOfMeasureId { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cartons { get; set; }
    public decimal Price { get; set; }
    public decimal TaxRate { get; set; } = 18m;
    public decimal TaxAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class InvoiceViewModel
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? BuyerAddress { get; set; }
    public int? ProvinceId { get; set; }
    public string? BuyerNTN { get; set; }
    public string? BuyerCNIC { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public InvoiceTypeEnum InvoiceType { get; set; }
    public SalesType SalesType { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FurtherTax { get; set; }
    public decimal FED { get; set; }
    public decimal ExtraTax { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal NetTotal { get; set; }
    public List<InvoiceLineViewModel> Lines { get; set; } = new();
}

public class BillLineViewModel
{
    public int ItemId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Cartons { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; } = 18m;
    public decimal TaxAmount { get; set; }
}

public class BillViewModel
{
    public int Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public DateTime BillDate { get; set; } = DateTime.Today;
    public string? RefNo { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public List<BillLineViewModel> Lines { get; set; } = new();
}

public class UserManagementViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<int> CompanyIds { get; set; } = new();
}

public class TaxSettingViewModel
{
    public int Id { get; set; }

    [Required]
    public string GroupName { get; set; } = string.Empty;

    public string? Description { get; set; }
    public decimal SalesTaxRate { get; set; }
    public decimal UnregisteredSalesTaxRate { get; set; }
}

public class BankViewModel
{
    public int Id { get; set; }

    [Required]
    public string BankName { get; set; } = string.Empty;

    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    public string? AccountTitle { get; set; }
    public string? Branch { get; set; }
    public string? IBAN { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BankTransactionViewModel
{
    public int BankId { get; set; }
    public BankTransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int? TransferToBankId { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

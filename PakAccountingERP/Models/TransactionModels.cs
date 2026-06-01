using PakAccountingERP.Models.Common;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.Models;

public class SalesInvoice : CompanyEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public string? BuyerAddress { get; set; }
    public int? ProvinceId { get; set; }
    public Province? Province { get; set; }
    public string? BuyerNTN { get; set; }
    public string? BuyerCNIC { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public InvoiceTypeEnum InvoiceType { get; set; } = InvoiceTypeEnum.SalesInvoice;
    public SalesType SalesType { get; set; } = SalesType.StandardRate;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal FurtherTax { get; set; }
    public decimal FED { get; set; }
    public decimal ExtraTax { get; set; }
    public decimal WithholdingTax { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCartons { get; set; }
    public bool IsPosted { get; set; }
    public FbrSubmissionStatus FbrStatus { get; set; } = FbrSubmissionStatus.Pending;
    public string? FbrInvoiceNumber { get; set; }
    public string? FbrResponseJson { get; set; }
    public string? FbrQrCodeData { get; set; }
    public DateTime? FbrSubmittedAt { get; set; }
    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}

public class SalesInvoiceItem : BaseEntity
{
    public int SalesInvoiceId { get; set; }
    public SalesInvoice? SalesInvoice { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public string? HSCode { get; set; }
    public string ProductDescription { get; set; } = string.Empty;
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure? UnitOfMeasure { get; set; }
    public decimal Quantity { get; set; }
    public decimal Cartons { get; set; }
    public decimal Price { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal LineTotal { get; set; }
}

public class Bill : CompanyEntity
{
    public string BillNumber { get; set; } = string.Empty;
    public int VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public string? RefNo { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCartons { get; set; }
    public bool IsPosted { get; set; }
    public ICollection<BillItem> Items { get; set; } = new List<BillItem>();
}

public class BillItem : BaseEntity
{
    public int BillId { get; set; }
    public Bill? Bill { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Cartons { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
}

public class Bank : CompanyEntity
{
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string? AccountTitle { get; set; }
    public string? Branch { get; set; }
    public string? IBAN { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public int? ChartOfAccountId { get; set; }
    public ChartOfAccount? ChartOfAccount { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<BankTransaction> Transactions { get; set; } = new List<BankTransaction>();
}

public class BankTransaction : CompanyEntity
{
    public int BankId { get; set; }
    public Bank? Bank { get; set; }
    public BankTransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int? TransferToBankId { get; set; }
    public Bank? TransferToBank { get; set; }
    public ChequeStatus? ChequeStatus { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public ReconciliationStatus ReconciliationStatus { get; set; } = ReconciliationStatus.Unreconciled;
}

public class BankReconciliation : CompanyEntity
{
    public int BankId { get; set; }
    public Bank? Bank { get; set; }
    public DateTime ReconciliationDate { get; set; }
    public decimal StatementBalance { get; set; }
    public decimal BookBalance { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
    public ICollection<BankReconciliationItem> Items { get; set; } = new List<BankReconciliationItem>();
}

public class BankReconciliationItem : BaseEntity
{
    public int BankReconciliationId { get; set; }
    public BankReconciliation? BankReconciliation { get; set; }
    public int? BankTransactionId { get; set; }
    public BankTransaction? BankTransaction { get; set; }
    public string? StatementReference { get; set; }
    public decimal Amount { get; set; }
    public bool IsMatched { get; set; }
}

public class JournalEntry : CompanyEntity
{
    public string EntryNumber { get; set; } = string.Empty;
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;
    public JournalEntryType EntryType { get; set; }
    public string? ReferenceNo { get; set; }
    public string? Description { get; set; }
    public bool IsPosted { get; set; }
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; set; }
    public JournalEntry? JournalEntry { get; set; }
    public int ChartOfAccountId { get; set; }
    public ChartOfAccount? ChartOfAccount { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Description { get; set; }
}

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public int? CompanyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? IPAddress { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
}

public class SystemSetting : BaseEntity
{
    public int? CompanyId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string? Category { get; set; }
}

public class DocumentSequence : CompanyEntity
{
    public string DocumentType { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public int LastNumber { get; set; }
    public int Padding { get; set; } = 6;
}

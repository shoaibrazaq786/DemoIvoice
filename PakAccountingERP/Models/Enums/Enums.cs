namespace PakAccountingERP.Models.Enums;

public enum AccountType
{
    AccountsReceivable = 1,
    AccountsPayable = 2,
    Asset = 3,
    Liability = 4,
    Income = 5,
    Expense = 6,
    FixedAsset = 7,
    Bank = 8,
    Equity = 9,
    OtherCurrentAsset = 10,
    LongTermLiability = 11,
    ShortTermLiability = 12,
    OtherIncome = 13,
    CostOfGoodsSold = 14
}

public enum CustomerType
{
    Registered = 1,
    Unregistered = 2
}

public enum SalesType
{
    GoodsAtReducedRate = 1,
    GoodsAtZeroRate = 2,
    StandardRate = 3
}

public enum InvoiceTypeEnum
{
    SalesInvoice = 1,
    DebitNote = 2,
    CreditNote = 3
}

public enum ItemType
{
    Inventory = 1,
    Service = 2,
    NonInventory = 3
}

public enum InventoryTransactionType
{
    StockIn = 1,
    StockOut = 2,
    OpeningStock = 3,
    Adjustment = 4,
    Transfer = 5
}

public enum CostingMethod
{
    FIFO = 1,
    Average = 2
}

public enum BankTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    Transfer = 3,
    Cheque = 4
}

public enum ChequeStatus
{
    Pending = 1,
    Cleared = 2,
    Bounced = 3,
    Cancelled = 4
}

public enum FbrSubmissionStatus
{
    Pending = 1,
    Submitted = 2,
    Success = 3,
    Failed = 4
}

public enum JournalEntryType
{
    Manual = 1,
    Invoice = 2,
    Bill = 3,
    Payment = 4,
    Receipt = 5,
    Inventory = 6
}

public enum ReconciliationStatus
{
    Unreconciled = 1,
    Reconciled = 2,
    Partial = 3
}

using PakAccountingERP.Models;

namespace PakAccountingERP.Interfaces;

public interface ICompanyContextService
{
    int? CurrentCompanyId { get; }
    Task SetCurrentCompanyAsync(int companyId);
    Task<IEnumerable<Company>> GetUserCompaniesAsync();
    Task<Company?> GetCurrentCompanyAsync();
}

public interface IAuditService
{
    Task LogAsync(string action, string? tableName = null, string? recordId = null,
        string? oldValue = null, string? newValue = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(int? companyId = null, int take = 100);
}

public interface IDocumentNumberService
{
    Task<string> GetNextNumberAsync(int companyId, string documentType);
}

public interface IFbrService
{
    Task<FbrSubmissionResult> SubmitInvoiceAsync(SalesInvoice invoice, Company company);
}

public class FbrSubmissionResult
{
    public bool Success { get; set; }
    public string? FbrInvoiceNumber { get; set; }
    public string? ResponseJson { get; set; }
    public string? QrCodeData { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IInvoiceService
{
    Task<SalesInvoice?> GetByIdAsync(int id);
    Task<IEnumerable<SalesInvoice>> GetAllAsync(int companyId);
    Task<SalesInvoice> CreateAsync(SalesInvoice invoice, IEnumerable<SalesInvoiceItem> items);
    Task UpdateAsync(SalesInvoice invoice, IEnumerable<SalesInvoiceItem> items);
    Task DeleteAsync(int id);
    Task PostInvoiceAsync(int id);
    Task<FbrSubmissionResult> SubmitToFbrAsync(int id);
}

public interface IInventoryService
{
    Task StockInAsync(int companyId, int itemId, int warehouseId, decimal quantity, decimal unitCost, string? batchNumber = null);
    Task StockOutAsync(int companyId, int itemId, int warehouseId, decimal quantity);
    Task AdjustStockAsync(int companyId, int itemId, int warehouseId, decimal quantity, string? notes);
    Task<decimal> GetStockValuationAsync(int companyId);
    Task<IEnumerable<Item>> GetLowStockItemsAsync(int companyId);
}

public interface IReportService
{
    Task<object> GetDailySalesAsync(int companyId, DateTime date);
    Task<object> GetMonthlySalesAsync(int companyId, int year, int month);
    Task<object> GetTrialBalanceAsync(int companyId, DateTime asOfDate);
    Task<object> GetProfitAndLossAsync(int companyId, DateTime from, DateTime to);
    Task<object> GetBalanceSheetAsync(int companyId, DateTime asOfDate);
    Task<byte[]> ExportToExcelAsync(string reportName, object data);
}

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(int companyId);
}

public class DashboardViewModel
{
    public decimal DailySales { get; set; }
    public decimal MonthlySales { get; set; }
    public decimal InventoryValue { get; set; }
    public decimal OutstandingReceivables { get; set; }
    public decimal OutstandingPayables { get; set; }
    public decimal TaxSummary { get; set; }
    public List<ChartDataPoint> SalesChart { get; set; } = new();
    public List<TopEntityViewModel> TopCustomers { get; set; } = new();
    public List<TopEntityViewModel> TopItems { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class TopEntityViewModel
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public interface IDatabaseBackupService
{
    Task<string> BackupDatabaseAsync();
}

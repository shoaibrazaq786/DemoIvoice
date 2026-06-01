using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using System.Text.Json;

namespace PakAccountingERP.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context) => _context = context;

    public async Task<object> GetDailySalesAsync(int companyId, DateTime date)
    {
        var invoices = await _context.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId && i.InvoiceDate.Date == date.Date && i.IsPosted)
            .Select(i => new { i.InvoiceNumber, Customer = i.Customer!.BuyerName, i.NetTotal, i.TaxAmount, i.InvoiceDate })
            .ToListAsync();
        return new { Date = date, Invoices = invoices, Total = invoices.Sum(i => i.NetTotal) };
    }

    public async Task<object> GetMonthlySalesAsync(int companyId, int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var invoices = await _context.SalesInvoices
            .Where(i => i.CompanyId == companyId && i.InvoiceDate >= start && i.InvoiceDate < end && i.IsPosted)
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.NetTotal), Tax = g.Sum(i => i.TaxAmount) })
            .OrderBy(g => g.Date)
            .ToListAsync();
        return new { Year = year, Month = month, DailyTotals = invoices, GrandTotal = invoices.Sum(i => i.Total) };
    }

    public async Task<object> GetTrialBalanceAsync(int companyId, DateTime asOfDate)
    {
        var lines = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.ChartOfAccount)
            .Where(l => l.JournalEntry!.CompanyId == companyId && l.JournalEntry.IsPosted && l.JournalEntry.EntryDate <= asOfDate)
            .GroupBy(l => new { l.ChartOfAccountId, l.ChartOfAccount!.AccountNumber, l.ChartOfAccount.AccountName })
            .Select(g => new
            {
                g.Key.AccountNumber,
                g.Key.AccountName,
                Debit = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            })
            .OrderBy(a => a.AccountNumber)
            .ToListAsync();

        return new { AsOfDate = asOfDate, Accounts = lines };
    }

    public async Task<object> GetProfitAndLossAsync(int companyId, DateTime from, DateTime to)
    {
        var accounts = await _context.JournalEntryLines
            .Include(l => l.JournalEntry)
            .Include(l => l.ChartOfAccount)
            .Where(l => l.JournalEntry!.CompanyId == companyId && l.JournalEntry.IsPosted
                && l.JournalEntry.EntryDate >= from && l.JournalEntry.EntryDate <= to)
            .GroupBy(l => l.ChartOfAccount!.AccountType)
            .Select(g => new { AccountType = g.Key.ToString(), Net = g.Sum(l => l.Credit - l.Debit) })
            .ToListAsync();

        return new { From = from, To = to, Accounts = accounts };
    }

    public async Task<object> GetBalanceSheetAsync(int companyId, DateTime asOfDate)
    {
        var tb = await GetTrialBalanceAsync(companyId, asOfDate);
        return tb;
    }

    public Task<byte[]> ExportToExcelAsync(string reportName, object data)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(reportName);
        ws.Cell(1, 1).Value = reportName;
        ws.Cell(2, 1).Value = JsonSerializer.Serialize(data);
        ws.Cell(2, 1).Style.Alignment.WrapText = true;
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}

public class DatabaseBackupService : IDatabaseBackupService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(IConfiguration configuration, ILogger<DatabaseBackupService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> BackupDatabaseAsync()
    {
        var connStr = _configuration.GetConnectionString("DefaultConnection")!;
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr);
        var dbName = builder.InitialCatalog;
        var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups", $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);

        _logger.LogInformation("Database backup requested for {Database} to {Path}", dbName, backupPath);
        // Requires SQL Server BACKUP DATABASE permission - run via sqlcmd in production
        return Task.FromResult(backupPath);
    }
}

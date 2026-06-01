using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentNumberService _docNumber;
    private readonly IFbrService _fbrService;
    private readonly IAuditService _auditService;

    public InvoiceService(ApplicationDbContext context, IDocumentNumberService docNumber,
        IFbrService fbrService, IAuditService auditService)
    {
        _context = context;
        _docNumber = docNumber;
        _fbrService = fbrService;
        _auditService = auditService;
    }

    public async Task<SalesInvoice?> GetByIdAsync(int id) =>
        await _context.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Province)
            .Include(i => i.Items).ThenInclude(li => li.Item)
            .Include(i => i.Items).ThenInclude(li => li.UnitOfMeasure)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<IEnumerable<SalesInvoice>> GetAllAsync(int companyId) =>
        await _context.SalesInvoices
            .Include(i => i.Customer)
            .Where(i => i.CompanyId == companyId)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

    public async Task<SalesInvoice> CreateAsync(SalesInvoice invoice, IEnumerable<SalesInvoiceItem> items)
    {
        invoice.InvoiceNumber = await _docNumber.GetNextNumberAsync(invoice.CompanyId, "Invoice");
        CalculateTotals(invoice, items);

        _context.SalesInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var item in items)
        {
            item.SalesInvoiceId = invoice.Id;
            _context.SalesInvoiceItems.Add(item);
        }
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Invoice Created", "SalesInvoices", invoice.Id.ToString());
        return invoice;
    }

    public async Task UpdateAsync(SalesInvoice invoice, IEnumerable<SalesInvoiceItem> items)
    {
        var existing = await GetByIdAsync(invoice.Id)
            ?? throw new InvalidOperationException("Invoice not found.");

        if (existing.IsPosted)
            throw new InvalidOperationException("Posted invoices cannot be edited.");

        CalculateTotals(invoice, items);

        _context.SalesInvoiceItems.RemoveRange(existing.Items);
        foreach (var item in items)
        {
            item.SalesInvoiceId = invoice.Id;
            _context.SalesInvoiceItems.Add(item);
        }

        _context.SalesInvoices.Update(invoice);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Invoice Updated", "SalesInvoices", invoice.Id.ToString());
    }

    public async Task DeleteAsync(int id)
    {
        var invoice = await GetByIdAsync(id);
        if (invoice == null) return;
        if (invoice.IsPosted) throw new InvalidOperationException("Posted invoices cannot be deleted.");

        invoice.IsDeleted = true;
        invoice.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Invoice Deleted", "SalesInvoices", id.ToString());
    }

    public async Task PostInvoiceAsync(int id)
    {
        var invoice = await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Invoice not found.");

        invoice.IsPosted = true;

        // Create journal entry (double-entry)
        var entry = new JournalEntry
        {
            CompanyId = invoice.CompanyId,
            EntryNumber = await _docNumber.GetNextNumberAsync(invoice.CompanyId, "Journal"),
            EntryDate = invoice.InvoiceDate,
            EntryType = JournalEntryType.Invoice,
            ReferenceNo = invoice.InvoiceNumber,
            Description = $"Sales Invoice {invoice.InvoiceNumber}",
            IsPosted = true
        };

        var arAccount = await GetAccountByTypeAsync(invoice.CompanyId, AccountType.AccountsReceivable);
        var salesAccount = await GetAccountByTypeAsync(invoice.CompanyId, AccountType.Income);
        var taxAccount = await GetAccountByTypeAsync(invoice.CompanyId, AccountType.ShortTermLiability);

        if (arAccount != null)
            entry.Lines.Add(new JournalEntryLine { ChartOfAccountId = arAccount.Id, Debit = invoice.NetTotal, Credit = 0 });
        if (salesAccount != null)
            entry.Lines.Add(new JournalEntryLine { ChartOfAccountId = salesAccount.Id, Debit = 0, Credit = invoice.SubTotal - invoice.DiscountAmount });
        if (taxAccount != null && invoice.TaxAmount > 0)
            entry.Lines.Add(new JournalEntryLine { ChartOfAccountId = taxAccount.Id, Debit = 0, Credit = invoice.TaxAmount });

        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Invoice Posted", "SalesInvoices", id.ToString());
    }

    public async Task<FbrSubmissionResult> SubmitToFbrAsync(int id)
    {
        var invoice = await GetByIdAsync(id)
            ?? throw new InvalidOperationException("Invoice not found.");
        var company = await _context.Companies.Include(c => c.Province)
            .FirstOrDefaultAsync(c => c.Id == invoice.CompanyId)
            ?? throw new InvalidOperationException("Company not found.");

        var result = await _fbrService.SubmitInvoiceAsync(invoice, company);

        invoice.FbrStatus = result.Success ? FbrSubmissionStatus.Success : FbrSubmissionStatus.Failed;
        invoice.FbrInvoiceNumber = result.FbrInvoiceNumber;
        invoice.FbrResponseJson = result.ResponseJson;
        invoice.FbrQrCodeData = result.QrCodeData;
        invoice.FbrSubmittedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return result;
    }

    private static void CalculateTotals(SalesInvoice invoice, IEnumerable<SalesInvoiceItem> items)
    {
        var itemList = items.ToList();
        foreach (var item in itemList)
        {
            var lineSubtotal = item.Quantity * item.Price;
            item.TaxAmount = Math.Round(lineSubtotal * item.TaxRate / 100, 2);
            item.LineTotal = Math.Round(lineSubtotal - item.Discount + item.TaxAmount, 2);
        }

        invoice.SubTotal = itemList.Sum(i => i.Quantity * i.Price);
        invoice.DiscountAmount = itemList.Sum(i => i.Discount);
        invoice.TaxAmount = itemList.Sum(i => i.TaxAmount);
        invoice.TotalQuantity = itemList.Sum(i => i.Quantity);
        invoice.TotalCartons = itemList.Sum(i => i.Cartons);
        invoice.NetTotal = Math.Round(invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount
            + invoice.FurtherTax + invoice.FED + invoice.ExtraTax - invoice.WithholdingTax, 2);
    }

    private async Task<ChartOfAccount?> GetAccountByTypeAsync(int companyId, AccountType type) =>
        await _context.ChartOfAccounts.FirstOrDefaultAsync(a => a.CompanyId == companyId && a.AccountType == type);
}

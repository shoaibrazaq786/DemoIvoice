using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;

namespace PakAccountingERP.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IInventoryService _inventoryService;

    public DashboardService(ApplicationDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(int companyId)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var invoices = await _context.SalesInvoices
            .Where(i => i.CompanyId == companyId && i.IsPosted)
            .ToListAsync();

        var dailySales = invoices.Where(i => i.InvoiceDate.Date == today).Sum(i => i.NetTotal);
        var monthlySales = invoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.NetTotal);

        var receivables = await _context.Customers
            .Where(c => c.CompanyId == companyId)
            .SumAsync(c => c.OpeningBalance);

        receivables += invoices.Sum(i => i.NetTotal);

        var payables = await _context.Vendors
            .Where(v => v.CompanyId == companyId)
            .SumAsync(v => v.OpeningBalance);

        payables += await _context.Bills
            .Where(b => b.CompanyId == companyId && b.IsPosted)
            .SumAsync(b => b.NetAmount);

        var salesChart = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .Select(date => new ChartDataPoint
            {
                Label = date.ToString("ddd"),
                Value = invoices.Where(inv => inv.InvoiceDate.Date == date).Sum(inv => inv.NetTotal)
            }).ToList();

        var topCustomers = await _context.SalesInvoices
            .Where(i => i.CompanyId == companyId && i.IsPosted)
            .GroupBy(i => i.Customer!.BuyerName)
            .Select(g => new TopEntityViewModel { Name = g.Key, Amount = g.Sum(i => i.NetTotal) })
            .OrderByDescending(t => t.Amount)
            .Take(5)
            .ToListAsync();

        var topItems = await _context.SalesInvoiceItems
            .Include(li => li.SalesInvoice)
            .Include(li => li.Item)
            .Where(li => li.SalesInvoice!.CompanyId == companyId && li.SalesInvoice.IsPosted)
            .GroupBy(li => li.Item!.ItemName)
            .Select(g => new TopEntityViewModel { Name = g.Key, Amount = g.Sum(li => li.LineTotal) })
            .OrderByDescending(t => t.Amount)
            .Take(5)
            .ToListAsync();

        return new DashboardViewModel
        {
            DailySales = dailySales,
            MonthlySales = monthlySales,
            InventoryValue = await _inventoryService.GetStockValuationAsync(companyId),
            OutstandingReceivables = receivables,
            OutstandingPayables = payables,
            TaxSummary = invoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.TaxAmount),
            SalesChart = salesChart,
            TopCustomers = topCustomers,
            TopItems = topItems
        };
    }
}

using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.Services;

public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public InventoryService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task StockInAsync(int companyId, int itemId, int warehouseId, decimal quantity,
        decimal unitCost, string? batchNumber = null)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId)
            ?? throw new InvalidOperationException("Item not found.");

        item.CurrentStock += quantity;

        var txn = new InventoryTransaction
        {
            CompanyId = companyId,
            ItemId = itemId,
            WarehouseId = warehouseId,
            TransactionType = InventoryTransactionType.StockIn,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            BalanceQuantity = item.CurrentStock,
            BatchNumber = batchNumber,
            TransactionDate = DateTime.UtcNow
        };
        _context.InventoryTransactions.Add(txn);

        if (!string.IsNullOrEmpty(batchNumber))
        {
            _context.InventoryBatches.Add(new InventoryBatch
            {
                CompanyId = companyId,
                ItemId = itemId,
                WarehouseId = warehouseId,
                BatchNumber = batchNumber,
                Quantity = quantity,
                UnitCost = unitCost,
                ReceivedDate = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Stock In", "Items", itemId.ToString(), null, $"Qty: {quantity}");
    }

    public async Task StockOutAsync(int companyId, int itemId, int warehouseId, decimal quantity)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId)
            ?? throw new InvalidOperationException("Item not found.");

        if (item.CurrentStock < quantity)
            throw new InvalidOperationException("Insufficient stock.");

        decimal unitCost;
        if (item.CostingMethod == CostingMethod.FIFO)
            unitCost = await GetFifoCostAsync(companyId, itemId, warehouseId, quantity);
        else
            unitCost = await GetAverageCostAsync(companyId, itemId);

        item.CurrentStock -= quantity;

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            CompanyId = companyId,
            ItemId = itemId,
            WarehouseId = warehouseId,
            TransactionType = InventoryTransactionType.StockOut,
            Quantity = quantity,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            BalanceQuantity = item.CurrentStock,
            TransactionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Stock Out", "Items", itemId.ToString(), null, $"Qty: {quantity}");
    }

    public async Task AdjustStockAsync(int companyId, int itemId, int warehouseId, decimal quantity, string? notes)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.CompanyId == companyId)
            ?? throw new InvalidOperationException("Item not found.");

        item.CurrentStock += quantity;

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            CompanyId = companyId,
            ItemId = itemId,
            WarehouseId = warehouseId,
            TransactionType = InventoryTransactionType.Adjustment,
            Quantity = Math.Abs(quantity),
            UnitCost = item.PurchaseRate,
            TotalCost = Math.Abs(quantity) * item.PurchaseRate,
            BalanceQuantity = item.CurrentStock,
            Notes = notes,
            TransactionDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Stock Adjustment", "Items", itemId.ToString());
    }

    public async Task<decimal> GetStockValuationAsync(int companyId)
    {
        return await _context.Items
            .Where(i => i.CompanyId == companyId && i.ItemType == ItemType.Inventory)
            .SumAsync(i => i.CurrentStock * i.PurchaseRate);
    }

    public async Task<IEnumerable<Item>> GetLowStockItemsAsync(int companyId) =>
        await _context.Items
            .Include(i => i.UnitOfMeasure)
            .Where(i => i.CompanyId == companyId && i.CurrentStock <= i.ReorderLevel)
            .OrderBy(i => i.CurrentStock)
            .ToListAsync();

    private async Task<decimal> GetFifoCostAsync(int companyId, int itemId, int warehouseId, decimal quantity)
    {
        var batches = await _context.InventoryBatches
            .Where(b => b.CompanyId == companyId && b.ItemId == itemId && b.WarehouseId == warehouseId && b.Quantity > 0)
            .OrderBy(b => b.ReceivedDate)
            .ToListAsync();

        decimal remaining = quantity;
        decimal totalCost = 0;
        foreach (var batch in batches)
        {
            if (remaining <= 0) break;
            var take = Math.Min(remaining, batch.Quantity);
            totalCost += take * batch.UnitCost;
            batch.Quantity -= take;
            remaining -= take;
        }
        return quantity > 0 ? totalCost / quantity : 0;
    }

    private async Task<decimal> GetAverageCostAsync(int companyId, int itemId)
    {
        var txns = await _context.InventoryTransactions
            .Where(t => t.CompanyId == companyId && t.ItemId == itemId &&
                        (t.TransactionType == InventoryTransactionType.StockIn ||
                         t.TransactionType == InventoryTransactionType.OpeningStock))
            .ToListAsync();

        if (!txns.Any()) return 0;
        var totalQty = txns.Sum(t => t.Quantity);
        return totalQty > 0 ? txns.Sum(t => t.TotalCost) / totalQty : 0;
    }
}

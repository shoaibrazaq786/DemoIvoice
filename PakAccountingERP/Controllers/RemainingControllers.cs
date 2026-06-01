using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using PakAccountingERP.Models.Enums;
using PakAccountingERP.Services;
using PakAccountingERP.ViewModels;

namespace PakAccountingERP.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;

    public InventoryController(ApplicationDbContext context, ICompanyContextService companyContext,
        IInventoryService inventoryService, IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _inventoryService = inventoryService;
        _auditService = auditService;
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        var items = await _context.Items
            .Include(i => i.UnitOfMeasure)
            .Include(i => i.ItemCategory)
            .Where(i => i.CompanyId == company.Id)
            .ToListAsync();
        return View(items);
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new ItemViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Inventory")]
    public async Task<IActionResult> Create(ItemViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        if (!ModelState.IsValid) { await LoadDropdowns(); return View(model); }

        _context.Items.Add(new Item
        {
            CompanyId = company.Id,
            ItemType = model.ItemType,
            ItemCode = model.ItemCode,
            ItemName = model.ItemName,
            Description = model.Description,
            HSCode = model.HSCode,
            Barcode = model.Barcode,
            UnitOfMeasureId = model.UnitOfMeasureId,
            ItemCategoryId = model.ItemCategoryId,
            PurchaseRate = model.PurchaseRate,
            SaleRate = model.SaleRate,
            MinimumStock = model.MinimumStock,
            CurrentStock = model.CurrentStock,
            ReorderLevel = model.ReorderLevel,
            CostingMethod = model.CostingMethod
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> StockIn()
    {
        await LoadStockDropdowns();
        return View();
    }

    [HttpPost]
    [RequireModule("Inventory")]
    public async Task<IActionResult> StockIn(int itemId, int warehouseId, decimal quantity, decimal unitCost, string? batchNumber)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return Json(ApiResponse<object>.Fail("No company"));
        await _inventoryService.StockInAsync(company.Id, itemId, warehouseId, quantity, unitCost, batchNumber);
        return Json(ApiResponse<object>.Ok(new { }, "Stock in recorded"));
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> Ledger(int itemId)
    {
        var txns = await _context.InventoryTransactions
            .Include(t => t.Warehouse)
            .Where(t => t.ItemId == itemId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
        ViewBag.Item = await _context.Items.FindAsync(itemId);
        return View(txns);
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> LowStock()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _inventoryService.GetLowStockItemsAsync(company!.Id));
    }

    [RequireModule("Inventory")]
    public async Task<IActionResult> Valuation()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.Value = await _inventoryService.GetStockValuationAsync(company!.Id);
        return View(await _context.Items.Where(i => i.CompanyId == company.Id).ToListAsync());
    }

    private async Task LoadDropdowns()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.Units = await _context.UnitsOfMeasure.ToListAsync();
        ViewBag.Categories = await _context.ItemCategories.Where(c => c.CompanyId == company!.Id).ToListAsync();
        ViewBag.ItemTypes = Enum.GetValues<ItemType>();
        ViewBag.CostingMethods = Enum.GetValues<CostingMethod>();
    }

    private async Task LoadStockDropdowns()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.Items = await _context.Items.Where(i => i.CompanyId == company!.Id).ToListAsync();
        ViewBag.Warehouses = await _context.Warehouses.Where(w => w.CompanyId == company!.Id).ToListAsync();
    }
}

[Authorize]
public class ChartOfAccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IAuditService _auditService;

    public ChartOfAccountsController(ApplicationDbContext context, ICompanyContextService companyContext,
        IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _auditService = auditService;
    }

    [RequireModule("ChartOfAccounts")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == company.Id)
            .OrderBy(a => a.AccountNumber)
            .ToListAsync();
        var tree = BuildTree(accounts, null);
        return View(tree);
    }

    [RequireModule("ChartOfAccounts")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new ChartOfAccountViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("ChartOfAccounts")]
    public async Task<IActionResult> Create(ChartOfAccountViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        if (!ModelState.IsValid) { await LoadDropdowns(); return View(model); }

        _context.ChartOfAccounts.Add(new ChartOfAccount
        {
            CompanyId = company.Id,
            AccountNumber = model.AccountNumber,
            AccountName = model.AccountName,
            ParentAccountId = model.ParentAccountId,
            AccountType = model.AccountType,
            Description = model.Description,
            IsActive = model.IsActive
        });
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Account Created", "ChartOfAccounts");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireModule("ChartOfAccounts")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await _context.ChartOfAccounts.FindAsync(id);
        if (account == null) return Json(ApiResponse<object>.Fail("Not found"));
        account.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Json(ApiResponse<object>.Ok(new { }));
    }

    private async Task LoadDropdowns()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.ParentAccounts = await _context.ChartOfAccounts.Where(a => a.CompanyId == company!.Id).ToListAsync();
        ViewBag.AccountTypes = Enum.GetValues<AccountType>();
    }

    private static List<ChartOfAccountViewModel> BuildTree(List<ChartOfAccount> accounts, int? parentId) =>
        accounts.Where(a => a.ParentAccountId == parentId).Select(a => new ChartOfAccountViewModel
        {
            Id = a.Id,
            AccountNumber = a.AccountNumber,
            AccountName = a.AccountName,
            ParentAccountId = a.ParentAccountId,
            AccountType = a.AccountType,
            Description = a.Description,
            IsActive = a.IsActive,
            Children = BuildTree(accounts, a.Id)
        }).ToList();
}

[Authorize]
public class BankingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IAuditService _auditService;

    public BankingController(ApplicationDbContext context, ICompanyContextService companyContext,
        IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _auditService = auditService;
    }

    [RequireModule("Banking")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _context.Banks.Where(b => b.CompanyId == company!.Id).ToListAsync());
    }

    [RequireModule("Banking")]
    public IActionResult Create() => View(new BankViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Banking")]
    public async Task<IActionResult> Create(BankViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (!ModelState.IsValid) return View(model);
        var bank = new Bank
        {
            CompanyId = company!.Id,
            BankName = model.BankName,
            AccountNumber = model.AccountNumber,
            AccountTitle = model.AccountTitle,
            Branch = model.Branch,
            IBAN = model.IBAN,
            OpeningBalance = model.OpeningBalance,
            CurrentBalance = model.OpeningBalance,
            IsActive = model.IsActive
        };
        _context.Banks.Add(bank);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [RequireModule("Banking")]
    public async Task<IActionResult> Transaction(int bankId)
    {
        ViewBag.Bank = await _context.Banks.FindAsync(bankId);
        ViewBag.Banks = await _context.Banks.Where(b => b.Id != bankId).ToListAsync();
        ViewBag.TransactionTypes = Enum.GetValues<BankTransactionType>();
        return View(new BankTransactionViewModel { BankId = bankId });
    }

    [HttpPost]
    [RequireModule("Banking")]
    public async Task<IActionResult> SaveTransaction(BankTransactionViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var bank = await _context.Banks.FindAsync(model.BankId);
        if (bank == null) return Json(ApiResponse<object>.Fail("Bank not found"));

        var txn = new BankTransaction
        {
            CompanyId = company!.Id,
            BankId = model.BankId,
            TransactionType = model.TransactionType,
            TransactionDate = model.TransactionDate,
            ReferenceNo = model.ReferenceNo,
            Description = model.Description,
            Amount = model.Amount,
            TransferToBankId = model.TransferToBankId,
            ChequeNumber = model.ChequeNumber,
            ChequeDate = model.ChequeDate,
            ChequeStatus = model.TransactionType == BankTransactionType.Cheque ? ChequeStatus.Pending : null
        };

        if (model.TransactionType is BankTransactionType.Deposit)
            bank.CurrentBalance += model.Amount;
        else if (model.TransactionType is BankTransactionType.Withdrawal or BankTransactionType.Cheque)
            bank.CurrentBalance -= model.Amount;
        else if (model.TransactionType == BankTransactionType.Transfer && model.TransferToBankId.HasValue)
        {
            bank.CurrentBalance -= model.Amount;
            var toBank = await _context.Banks.FindAsync(model.TransferToBankId);
            if (toBank != null) toBank.CurrentBalance += model.Amount;
        }

        _context.BankTransactions.Add(txn);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Bank Transaction", "BankTransactions", txn.Id.ToString());
        return Json(ApiResponse<object>.Ok(new { txn.Id }));
    }

    [RequireModule("Banking")]
    public async Task<IActionResult> Ledger(int bankId)
    {
        ViewBag.Bank = await _context.Banks.FindAsync(bankId);
        var txns = await _context.BankTransactions.Where(t => t.BankId == bankId).OrderBy(t => t.TransactionDate).ToListAsync();
        return View(txns);
    }
}

[Authorize]
public class BankReconciliationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;

    public BankReconciliationController(ApplicationDbContext context, ICompanyContextService companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    [RequireModule("BankReconciliation")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _context.BankReconciliations
            .Include(r => r.Bank)
            .Where(r => r.CompanyId == company!.Id)
            .ToListAsync());
    }

    [RequireModule("BankReconciliation")]
    public async Task<IActionResult> Create(int bankId)
    {
        var bank = await _context.Banks.FindAsync(bankId);
        ViewBag.Bank = bank;
        ViewBag.Unreconciled = await _context.BankTransactions
            .Where(t => t.BankId == bankId && t.ReconciliationStatus == ReconciliationStatus.Unreconciled)
            .ToListAsync();
        return View();
    }
}

[Authorize]
public class TaxSettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;

    public TaxSettingsController(ApplicationDbContext context, ICompanyContextService companyContext)
    {
        _context = context;
        _companyContext = companyContext;
    }

    [RequireModule("TaxSettings")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _context.TaxSettings.Where(t => t.CompanyId == company!.Id).ToListAsync());
    }

    [RequireModule("TaxSettings")]
    public IActionResult Create() => View(new TaxSettingViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("TaxSettings")]
    public async Task<IActionResult> Create(TaxSettingViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (!ModelState.IsValid) return View(model);
        _context.TaxSettings.Add(new TaxSetting
        {
            CompanyId = company!.Id,
            GroupName = model.GroupName,
            Description = model.Description,
            SalesTaxRate = model.SalesTaxRate,
            UnregisteredSalesTaxRate = model.UnregisteredSalesTaxRate
        });
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly ICompanyContextService _companyContext;

    public ReportsController(IReportService reportService, ICompanyContextService companyContext)
    {
        _reportService = reportService;
        _companyContext = companyContext;
    }

    [RequireModule("Reports")]
    public IActionResult Index() => View();

    [RequireModule("Reports")]
    public async Task<IActionResult> DailySales(DateTime? date)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var report = await _reportService.GetDailySalesAsync(company!.Id, date ?? DateTime.Today);
        return View(report);
    }

    [RequireModule("Reports")]
    public async Task<IActionResult> MonthlySales(int? year, int? month)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        return View(await _reportService.GetMonthlySalesAsync(company!.Id, y, m));
    }

    [RequireModule("Reports")]
    public async Task<IActionResult> TrialBalance(DateTime? asOf)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _reportService.GetTrialBalanceAsync(company!.Id, asOf ?? DateTime.Today));
    }

    [RequireModule("Reports")]
    public async Task<IActionResult> ProfitAndLoss(DateTime? from, DateTime? to)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var f = from ?? new DateTime(DateTime.Today.Year, 1, 1);
        var t = to ?? DateTime.Today;
        return View(await _reportService.GetProfitAndLossAsync(company!.Id, f, t));
    }

    [RequireModule("Reports")]
    public async Task<IActionResult> BalanceSheet(DateTime? asOf)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _reportService.GetBalanceSheetAsync(company!.Id, asOf ?? DateTime.Today));
    }

    [RequireModule("Reports")]
    public async Task<IActionResult> SalesTax(DateTime? from, DateTime? to)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var f = from ?? DateTime.Today.AddMonths(-1);
        var t = to ?? DateTime.Today;
        var invoices = await _reportService.GetDailySalesAsync(company!.Id, f);
        ViewBag.From = f;
        ViewBag.To = t;
        return View(invoices);
    }

    [HttpGet]
    [RequireModule("Reports")]
    public async Task<IActionResult> ExportExcel(string reportName, DateTime? date)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        var data = await _reportService.GetDailySalesAsync(company!.Id, date ?? DateTime.Today);
        var bytes = await _reportService.ExportToExcelAsync(reportName, data);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{reportName}.xlsx");
    }
}

[Authorize(Roles = "SuperAdmin,Admin")]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDatabaseBackupService _backupService;

    public SettingsController(ApplicationDbContext context, IDatabaseBackupService backupService)
    {
        _context = context;
        _backupService = backupService;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> CompanySettings()
    {
        return View(await _context.Companies.ToListAsync());
    }

    public IActionResult SecuritySettings() => View();

    [HttpPost]
    public async Task<IActionResult> BackupDatabase()
    {
        var path = await _backupService.BackupDatabaseAsync();
        return Json(ApiResponse<object>.Ok(new { path }, "Backup initiated"));
    }
}

[Authorize(Roles = "SuperAdmin,Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context, IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.ToListAsync();
        var vms = new List<UserManagementViewModel>();
        foreach (var user in users)
        {
            vms.Add(new UserManagementViewModel
            {
                Id = user.Id,
                Email = user.Email!,
                FullName = user.FullName,
                IsActive = user.IsActive,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                CompanyIds = await _context.UserCompanies.Where(uc => uc.UserId == user.Id).Select(uc => uc.CompanyId).ToListAsync()
            });
        }
        ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
        ViewBag.Companies = await _context.Companies.ToListAsync();
        return View(vms);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(string email, string password, string fullName, string role, int companyId)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded) return Json(ApiResponse<object>.Fail(string.Join(", ", result.Errors.Select(e => e.Description))));

        await _userManager.AddToRoleAsync(user, role);
        _context.UserCompanies.Add(new UserCompany { UserId = user.Id, CompanyId = companyId });
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("User Created", "Users", user.Id);
        return Json(ApiResponse<object>.Ok(new { user.Id }));
    }
}

[Authorize]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;
    private readonly ICompanyContextService _companyContext;

    public AuditLogsController(IAuditService auditService, ICompanyContextService companyContext)
    {
        _auditService = auditService;
        _companyContext = companyContext;
    }

    [RequireModule("AuditLogs")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        return View(await _auditService.GetLogsAsync(company?.Id));
    }
}

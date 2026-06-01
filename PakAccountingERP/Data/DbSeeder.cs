using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Models;
using PakAccountingERP.Models.Enums;

namespace PakAccountingERP.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedProvincesAsync(context);
        await SeedUnitsOfMeasureAsync(context);
        await SeedPermissionsAsync(context);
        await SeedDefaultCompanyAsync(context, userManager);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["SuperAdmin", "Admin", "Accountant", "SalesUser", "PurchaseUser", "ReportsUser"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedProvincesAsync(ApplicationDbContext context)
    {
        if (await context.Provinces.AnyAsync()) return;

        var provinces = new[]
        {
            "AZD JAMMU AND KASHMIR", "BALOCHISTAN", "CAPITAL TERRITORY", "FATA/PATA",
            "GILGIT BALTISTAN", "KHYBER PAKTUNKHWA", "PUNJAB", "SINDH"
        };

        for (var i = 0; i < provinces.Length; i++)
        {
            context.Provinces.Add(new Province { Name = provinces[i], Code = $"P{i + 1:D2}" });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitsOfMeasureAsync(ApplicationDbContext context)
    {
        if (await context.UnitsOfMeasure.AnyAsync()) return;

        var units = new[] { "KG", "Pound", "Per Piece", "Cartons" };
        foreach (var unit in units)
            context.UnitsOfMeasure.Add(new UnitOfMeasure { Name = unit, Symbol = unit });
        await context.SaveChangesAsync();
    }

    private static async Task SeedPermissionsAsync(ApplicationDbContext context)
    {
        if (await context.Permissions.AnyAsync()) return;

        var modules = new[]
        {
            "Dashboard", "Company", "Settings", "Invoice", "Bills", "Banking", "Reports",
            "Inventory", "Customers", "Vendors", "ChartOfAccounts", "TaxSettings",
            "AuditLogs", "UserManagement", "BankReconciliation"
        };

        foreach (var module in modules)
        {
            context.Permissions.Add(new Permission
            {
                Name = $"{module}.FullAccess",
                Module = module,
                Description = $"Full access to {module} module"
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultCompanyAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        if (await context.Companies.AnyAsync()) return;

        var punjab = await context.Provinces.FirstOrDefaultAsync(p => p.Name == "PUNJAB");

        var company = new Company
        {
            CompanyName = "Demo Company (Pvt) Ltd",
            Address = "123 Main Boulevard, Lahore",
            NTN = "1234567-8",
            STRN = "STRN-123456",
            ProvinceId = punjab?.Id,
            Phone = "+92-42-1234567",
            Email = "info@democompany.pk",
            FbrHttpPostUrl = "https://gw.fbr.gov.pk/pdi/v1/api/Live/PostData",
            IsDefault = true
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Seed document sequences
        var docTypes = new[] { "Invoice", "Bill", "Customer", "Vendor", "Journal", "BankTxn" };
        foreach (var docType in docTypes)
        {
            context.DocumentSequences.Add(new DocumentSequence
            {
                CompanyId = company.Id,
                DocumentType = docType,
                Prefix = docType.Substring(0, 1).ToUpper(),
                LastNumber = 0,
                Padding = 6
            });
        }

        // Seed default chart of accounts
        SeedDefaultChartOfAccounts(context, company.Id);

        // Seed default tax setting
        context.TaxSettings.Add(new TaxSetting
        {
            CompanyId = company.Id,
            GroupName = "Standard Sales Tax",
            Description = "Default 18% sales tax",
            SalesTaxRate = 18m,
            UnregisteredSalesTaxRate = 18m
        });

        // Seed default warehouse
        context.Warehouses.Add(new Warehouse
        {
            CompanyId = company.Id,
            Name = "Main Warehouse",
            Location = "Lahore",
            IsActive = true
        });

        await context.SaveChangesAsync();

        // Create super admin user
        const string adminEmail = "admin@pakaccounting.pk";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "SuperAdmin");
                context.UserCompanies.Add(new UserCompany
                {
                    UserId = admin.Id,
                    CompanyId = company.Id,
                    IsDefault = true
                });
                await context.SaveChangesAsync();
            }
        }
    }

    private static void SeedDefaultChartOfAccounts(ApplicationDbContext context, int companyId)
    {
        var accounts = new (string Number, string Name, AccountType Type)[]
        {
            ("1000", "Assets", AccountType.Asset),
            ("1100", "Cash", AccountType.Asset),
            ("1200", "Accounts Receivable", AccountType.AccountsReceivable),
            ("1300", "Inventory", AccountType.OtherCurrentAsset),
            ("1400", "Fixed Assets", AccountType.FixedAsset),
            ("2000", "Liabilities", AccountType.Liability),
            ("2100", "Accounts Payable", AccountType.AccountsPayable),
            ("2200", "Sales Tax Payable", AccountType.ShortTermLiability),
            ("3000", "Equity", AccountType.Equity),
            ("3100", "Retained Earnings", AccountType.Equity),
            ("4000", "Income", AccountType.Income),
            ("4100", "Sales Revenue", AccountType.Income),
            ("5000", "Expenses", AccountType.Expense),
            ("5100", "Cost of Goods Sold", AccountType.CostOfGoodsSold),
            ("5200", "Operating Expenses", AccountType.Expense),
            ("6000", "Bank Accounts", AccountType.Bank)
        };

        foreach (var (number, name, type) in accounts)
        {
            context.ChartOfAccounts.Add(new ChartOfAccount
            {
                CompanyId = companyId,
                AccountNumber = number,
                AccountName = name,
                AccountType = type,
                IsActive = true
            });
        }
    }
}

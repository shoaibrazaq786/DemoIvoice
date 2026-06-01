using Microsoft.AspNetCore.Authorization;
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
public class CustomersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IDocumentNumberService _docNumber;
    private readonly IAuditService _auditService;

    public CustomersController(ApplicationDbContext context, ICompanyContextService companyContext,
        IDocumentNumberService docNumber, IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _docNumber = docNumber;
        _auditService = auditService;
    }

    [RequireModule("Customers")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        var customers = await _context.Customers
            .Include(c => c.Province)
            .Where(c => c.CompanyId == company.Id)
            .OrderBy(c => c.BuyerName)
            .ToListAsync();
        return View(customers);
    }

    [RequireModule("Customers")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new CustomerViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Customers")]
    public async Task<IActionResult> Create(CustomerViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        if (!ModelState.IsValid) { await LoadDropdowns(); return View(model); }

        var customer = MapToEntity(model, company.Id);
        customer.BuyerId = await _docNumber.GetNextNumberAsync(company.Id, "Customer");
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Customer Created", "Customers", customer.Id.ToString());
        return RedirectToAction(nameof(Index));
    }

    [RequireModule("Customers")]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        await LoadDropdowns();
        return View(MapToViewModel(customer));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Customers")]
    public async Task<IActionResult> Edit(CustomerViewModel model)
    {
        if (!ModelState.IsValid) { await LoadDropdowns(); return View(model); }
        var customer = await _context.Customers.FindAsync(model.Id);
        if (customer == null) return NotFound();

        customer.BuyerName = model.BuyerName;
        customer.OpeningBalance = model.OpeningBalance;
        customer.Address = model.Address;
        customer.ProvinceId = model.ProvinceId;
        customer.Phone = model.Phone;
        customer.Mobile = model.Mobile;
        customer.Email = model.Email;
        customer.NTN = model.NTN;
        customer.CNIC = model.CNIC;
        customer.STRN = model.STRN;
        customer.CustomerType = model.CustomerType;
        customer.SalesType = model.SalesType;
        customer.InvoiceType = model.InvoiceType;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Customer Updated", "Customers", customer.Id.ToString());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireModule("Customers")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return Json(ApiResponse<object>.Fail("Not found"));
        customer.IsDeleted = true;
        customer.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Customer Deleted", "Customers", id.ToString());
        return Json(ApiResponse<object>.Ok(new { }, "Deleted"));
    }

    [RequireModule("Customers")]
    public async Task<IActionResult> Ledger(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();
        var invoices = await _context.SalesInvoices
            .Where(i => i.CustomerId == id && i.IsPosted)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync();
        ViewBag.Customer = customer;
        return View(invoices);
    }

    private async Task LoadDropdowns()
    {
        ViewBag.Provinces = await _context.Provinces.ToListAsync();
        ViewBag.CustomerTypes = Enum.GetValues<CustomerType>();
        ViewBag.SalesTypes = Enum.GetValues<SalesType>();
        ViewBag.InvoiceTypes = Enum.GetValues<InvoiceTypeEnum>();
    }

    private static Customer MapToEntity(CustomerViewModel m, int companyId) => new()
    {
        CompanyId = companyId,
        BuyerName = m.BuyerName,
        OpeningBalance = m.OpeningBalance,
        Address = m.Address,
        ProvinceId = m.ProvinceId,
        Phone = m.Phone,
        Mobile = m.Mobile,
        Email = m.Email,
        NTN = m.NTN,
        CNIC = m.CNIC,
        STRN = m.STRN,
        CustomerType = m.CustomerType,
        SalesType = m.SalesType,
        InvoiceType = m.InvoiceType
    };

    private static CustomerViewModel MapToViewModel(Customer c) => new()
    {
        Id = c.Id,
        BuyerId = c.BuyerId,
        BuyerName = c.BuyerName,
        OpeningBalance = c.OpeningBalance,
        Address = c.Address,
        ProvinceId = c.ProvinceId,
        Phone = c.Phone,
        Mobile = c.Mobile,
        Email = c.Email,
        NTN = c.NTN,
        CNIC = c.CNIC,
        STRN = c.STRN,
        CustomerType = c.CustomerType,
        SalesType = c.SalesType,
        InvoiceType = c.InvoiceType
    };
}

[Authorize]
public class VendorsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IDocumentNumberService _docNumber;
    private readonly IAuditService _auditService;

    public VendorsController(ApplicationDbContext context, ICompanyContextService companyContext,
        IDocumentNumberService docNumber, IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _docNumber = docNumber;
        _auditService = auditService;
    }

    [RequireModule("Vendors")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        return View(await _context.Vendors.Where(v => v.CompanyId == company.Id).OrderBy(v => v.VendorName).ToListAsync());
    }

    [RequireModule("Vendors")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Provinces = await _context.Provinces.ToListAsync();
        return View(new VendorViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Vendors")]
    public async Task<IActionResult> Create(VendorViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        if (!ModelState.IsValid) { ViewBag.Provinces = await _context.Provinces.ToListAsync(); return View(model); }

        var vendor = new Vendor
        {
            CompanyId = company.Id,
            VendorCode = await _docNumber.GetNextNumberAsync(company.Id, "Vendor"),
            VendorName = model.VendorName,
            OpeningBalance = model.OpeningBalance,
            Address = model.Address,
            ProvinceId = model.ProvinceId,
            Phone = model.Phone,
            Email = model.Email,
            NTN = model.NTN,
            STRN = model.STRN,
            DefaultSalesTaxRate = model.DefaultSalesTaxRate
        };
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Vendor Created", "Vendors", vendor.Id.ToString());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireModule("Vendors")]
    public async Task<IActionResult> Delete(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null) return Json(ApiResponse<object>.Fail("Not found"));
        vendor.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Json(ApiResponse<object>.Ok(new { }));
    }
}

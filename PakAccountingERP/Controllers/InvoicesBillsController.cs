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
public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;

    public InvoicesController(IInvoiceService invoiceService, ApplicationDbContext context,
        ICompanyContextService companyContext)
    {
        _invoiceService = invoiceService;
        _context = context;
        _companyContext = companyContext;
    }

    [RequireModule("Invoice")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        return View(await _invoiceService.GetAllAsync(company.Id));
    }

    [RequireModule("Invoice")]
    public async Task<IActionResult> Create()
    {
        await LoadDropdowns();
        return View(new InvoiceViewModel { Lines = [new InvoiceLineViewModel()] });
    }

    [HttpPost]
    [RequireModule("Invoice")]
    public async Task<IActionResult> Save([FromBody] InvoiceViewModel model)
    {
        try
        {
            var company = await _companyContext.GetCurrentCompanyAsync();
            if (company == null) return Json(ApiResponse<object>.Fail("No company selected"));

            var invoice = new SalesInvoice
            {
                CompanyId = company.Id,
                CustomerId = model.CustomerId,
                BuyerAddress = model.BuyerAddress,
                ProvinceId = model.ProvinceId,
                BuyerNTN = model.BuyerNTN,
                BuyerCNIC = model.BuyerCNIC,
                InvoiceDate = model.InvoiceDate,
                InvoiceType = model.InvoiceType,
                SalesType = model.SalesType,
                FurtherTax = model.FurtherTax,
                FED = model.FED,
                ExtraTax = model.ExtraTax,
                WithholdingTax = model.WithholdingTax
            };

            var items = model.Lines.Select(l => new SalesInvoiceItem
            {
                ItemId = l.ItemId,
                HSCode = l.HSCode,
                ProductDescription = l.ProductDescription,
                UnitOfMeasureId = l.UnitOfMeasureId,
                Quantity = l.Quantity,
                Cartons = l.Cartons,
                Price = l.Price,
                TaxRate = l.TaxRate,
                Discount = l.Discount
            });

            var result = model.Id > 0
                ? await UpdateInvoice(model, items)
                : await _invoiceService.CreateAsync(invoice, items);

            return Json(ApiResponse<object>.Ok(new { result.Id, result.InvoiceNumber }, "Invoice saved"));
        }
        catch (Exception ex)
        {
            return Json(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [RequireModule("Invoice")]
    public async Task<IActionResult> Edit(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        await LoadDropdowns();

        var vm = new InvoiceViewModel
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            BuyerAddress = invoice.BuyerAddress,
            ProvinceId = invoice.ProvinceId,
            BuyerNTN = invoice.BuyerNTN,
            BuyerCNIC = invoice.BuyerCNIC,
            InvoiceDate = invoice.InvoiceDate,
            InvoiceType = invoice.InvoiceType,
            SalesType = invoice.SalesType,
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxAmount = invoice.TaxAmount,
            FurtherTax = invoice.FurtherTax,
            FED = invoice.FED,
            ExtraTax = invoice.ExtraTax,
            WithholdingTax = invoice.WithholdingTax,
            NetTotal = invoice.NetTotal,
            Lines = invoice.Items.Select(i => new InvoiceLineViewModel
            {
                ItemId = i.ItemId,
                HSCode = i.HSCode,
                ProductDescription = i.ProductDescription,
                UnitOfMeasureId = i.UnitOfMeasureId,
                Quantity = i.Quantity,
                Cartons = i.Cartons,
                Price = i.Price,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                Discount = i.Discount,
                LineTotal = i.LineTotal
            }).ToList()
        };
        return View("Create", vm);
    }

    [RequireModule("Invoice")]
    public async Task<IActionResult> Print(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.Company = company;
        return View(invoice);
    }

    [HttpPost]
    [RequireModule("Invoice")]
    public async Task<IActionResult> Post(int id)
    {
        try
        {
            await _invoiceService.PostInvoiceAsync(id);
            return Json(ApiResponse<object>.Ok(new { }, "Invoice posted"));
        }
        catch (Exception ex) { return Json(ApiResponse<object>.Fail(ex.Message)); }
    }

    [HttpPost]
    [RequireModule("Invoice")]
    public async Task<IActionResult> SubmitFbr(int id)
    {
        var result = await _invoiceService.SubmitToFbrAsync(id);
        return Json(result.Success
            ? ApiResponse<object>.Ok(new { result.FbrInvoiceNumber, result.QrCodeData })
            : ApiResponse<object>.Fail(result.ErrorMessage ?? "FBR submission failed"));
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomerDetails(int id)
    {
        var customer = await _context.Customers.Include(c => c.Province).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        return Json(new
        {
            customer.BuyerName,
            customer.Address,
            ProvinceId = customer.ProvinceId,
            customer.NTN,
            customer.CNIC,
            customer.CustomerType,
            customer.SalesType,
            customer.InvoiceType
        });
    }

    private async Task<SalesInvoice> UpdateInvoice(InvoiceViewModel model, IEnumerable<SalesInvoiceItem> items)
    {
        var invoice = await _invoiceService.GetByIdAsync(model.Id)
            ?? throw new InvalidOperationException("Invoice not found");
        invoice.CustomerId = model.CustomerId;
        invoice.BuyerAddress = model.BuyerAddress;
        invoice.ProvinceId = model.ProvinceId;
        invoice.BuyerNTN = model.BuyerNTN;
        invoice.BuyerCNIC = model.BuyerCNIC;
        invoice.InvoiceDate = model.InvoiceDate;
        invoice.InvoiceType = model.InvoiceType;
        invoice.SalesType = model.SalesType;
        invoice.FurtherTax = model.FurtherTax;
        invoice.FED = model.FED;
        invoice.ExtraTax = model.ExtraTax;
        invoice.WithholdingTax = model.WithholdingTax;
        await _invoiceService.UpdateAsync(invoice, items);
        return invoice;
    }

    private async Task LoadDropdowns()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return;
        ViewBag.Customers = await _context.Customers.Where(c => c.CompanyId == company.Id).ToListAsync();
        ViewBag.Items = await _context.Items.Include(i => i.UnitOfMeasure).Where(i => i.CompanyId == company.Id).ToListAsync();
        ViewBag.Units = await _context.UnitsOfMeasure.ToListAsync();
        ViewBag.Provinces = await _context.Provinces.ToListAsync();
        ViewBag.InvoiceTypes = Enum.GetValues<InvoiceTypeEnum>();
        ViewBag.SalesTypes = Enum.GetValues<SalesType>();
    }

    [HttpGet]
    public IActionResult QrCode(string data)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.Q);
        var png = new QRCoder.PngByteQRCode(qrData);
        return File(png.GetGraphic(5), "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        var company = await _companyContext.GetCurrentCompanyAsync();
        var pdfService = HttpContext.RequestServices.GetRequiredService<InvoicePdfService>();
        return File(pdfService.GenerateInvoicePdf(invoice, company!), "application/pdf", $"{invoice.InvoiceNumber}.pdf");
    }
}

[Authorize]
public class BillsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICompanyContextService _companyContext;
    private readonly IDocumentNumberService _docNumber;
    private readonly IAuditService _auditService;

    public BillsController(ApplicationDbContext context, ICompanyContextService companyContext,
        IDocumentNumberService docNumber, IAuditService auditService)
    {
        _context = context;
        _companyContext = companyContext;
        _docNumber = docNumber;
        _auditService = auditService;
    }

    [RequireModule("Bills")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");
        var bills = await _context.Bills.Include(b => b.Vendor).Where(b => b.CompanyId == company.Id).ToListAsync();
        return View(bills);
    }

    [RequireModule("Bills")]
    public async Task<IActionResult> Create()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        ViewBag.Vendors = await _context.Vendors.Where(v => v.CompanyId == company!.Id).ToListAsync();
        ViewBag.Items = await _context.Items.Where(i => i.CompanyId == company!.Id).ToListAsync();
        return View(new BillViewModel { Lines = [new BillLineViewModel()] });
    }

    [HttpPost]
    [RequireModule("Bills")]
    public async Task<IActionResult> Save([FromBody] BillViewModel model)
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return Json(ApiResponse<object>.Fail("No company"));

        var bill = new Bill
        {
            CompanyId = company.Id,
            BillNumber = await _docNumber.GetNextNumberAsync(company.Id, "Bill"),
            VendorId = model.VendorId,
            BillDate = model.BillDate,
            RefNo = model.RefNo
        };

        foreach (var line in model.Lines)
        {
            var amount = line.Quantity * line.Rate;
            var tax = Math.Round(amount * line.TaxRate / 100, 2);
            bill.Items.Add(new BillItem
            {
                ItemId = line.ItemId,
                Description = line.Description,
                Quantity = line.Quantity,
                Cartons = line.Cartons,
                Rate = line.Rate,
                Amount = amount,
                TaxRate = line.TaxRate,
                TaxAmount = tax
            });
        }

        bill.SubTotal = bill.Items.Sum(i => i.Amount);
        bill.TaxAmount = bill.Items.Sum(i => i.TaxAmount);
        bill.NetAmount = bill.SubTotal + bill.TaxAmount;
        bill.TotalQuantity = bill.Items.Sum(i => i.Quantity);
        bill.TotalCartons = bill.Items.Sum(i => i.Cartons);

        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Bill Created", "Bills", bill.Id.ToString());
        return Json(ApiResponse<object>.Ok(new { bill.Id, bill.BillNumber }));
    }
}

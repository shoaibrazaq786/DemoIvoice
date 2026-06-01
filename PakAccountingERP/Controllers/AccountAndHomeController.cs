using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using PakAccountingERP.Services;
using PakAccountingERP.ViewModels;

namespace PakAccountingERP.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountController(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await _auditService.LogAsync("User Login", "Users", user.Id);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _auditService.LogAsync("User Logout");
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}

[Authorize]
public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ICompanyContextService _companyContext;

    public HomeController(IDashboardService dashboardService, ICompanyContextService companyContext)
    {
        _dashboardService = dashboardService;
        _companyContext = companyContext;
    }

    [RequireModule("Dashboard")]
    public async Task<IActionResult> Index()
    {
        var company = await _companyContext.GetCurrentCompanyAsync();
        if (company == null) return RedirectToAction("Index", "Company");

        var data = await _dashboardService.GetDashboardDataAsync(company.Id);
        ViewBag.CompanyName = company.CompanyName;
        return View(data);
    }

    public IActionResult Error() => View();
}

[Authorize]
public class CompanyController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ICompanyContextService _companyContext;
    private readonly IAuditService _auditService;
    private readonly ApplicationDbContext _context;

    public CompanyController(IUnitOfWork uow, ICompanyContextService companyContext,
        IAuditService auditService, ApplicationDbContext context)
    {
        _uow = uow;
        _companyContext = companyContext;
        _auditService = auditService;
        _context = context;
    }

    [RequireModule("Company")]
    public async Task<IActionResult> Index() =>
        View(await _context.Companies.Include(c => c.Province).ToListAsync());

    [RequireModule("Company")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Provinces = await _context.Provinces.ToListAsync();
        return View(new CompanyViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequireModule("Company")]
    public async Task<IActionResult> Create(CompanyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Provinces = await _context.Provinces.ToListAsync();
            return View(model);
        }

        var company = new Company
        {
            CompanyName = model.CompanyName,
            Address = model.Address,
            NTN = model.NTN,
            STRN = model.STRN,
            ProvinceId = model.ProvinceId,
            Phone = model.Phone,
            Email = model.Email,
            FbrHttpPostUrl = model.FbrHttpPostUrl,
            ApiToken = model.ApiToken,
            IsDefault = model.IsDefault
        };

        if (model.Logo != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Logo.FileName)}";
            var path = Path.Combine("wwwroot", "uploads", "logos", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var stream = new FileStream(path, FileMode.Create);
            await model.Logo.CopyToAsync(stream);
            company.LogoPath = $"/uploads/logos/{fileName}";
        }

        await _uow.Repository<Company>().AddAsync(company);
        await _uow.SaveChangesAsync();
        await _auditService.LogAsync("Company Created", "Companies", company.Id.ToString());
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SwitchCompany(int companyId)
    {
        await _companyContext.SetCurrentCompanyAsync(companyId);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> GetUserCompanies()
    {
        var companies = await _companyContext.GetUserCompaniesAsync();
        var current = _companyContext.CurrentCompanyId;
        return Json(companies.Select(c => new { c.Id, c.CompanyName, IsSelected = c.Id == current }));
    }
}

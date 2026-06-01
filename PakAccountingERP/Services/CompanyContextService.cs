using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using System.Security.Claims;

namespace PakAccountingERP.Services;

public class CompanyContextService : ICompanyContextService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SessionKey = "CurrentCompanyId";

    public CompanyContextService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public int? CurrentCompanyId
    {
        get => _httpContextAccessor.HttpContext?.Session.GetInt32(SessionKey);
    }

    public async Task SetCurrentCompanyAsync(int companyId)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return;

        var hasAccess = await _context.UserCompanies
            .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId && !uc.IsDeleted);
        if (!hasAccess && !_httpContextAccessor.HttpContext!.User.IsInRole("SuperAdmin")) return;

        _httpContextAccessor.HttpContext!.Session.SetInt32(SessionKey, companyId);
    }

    public async Task<IEnumerable<Company>> GetUserCompaniesAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Enumerable.Empty<Company>();

        if (user.IsInRole("SuperAdmin"))
            return await _context.Companies.Where(c => !c.IsDeleted).OrderBy(c => c.CompanyName).ToListAsync();

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await _context.UserCompanies
            .Where(uc => uc.UserId == userId && !uc.IsDeleted)
            .Include(uc => uc.Company)
            .Select(uc => uc.Company!)
            .OrderBy(c => c.CompanyName)
            .ToListAsync();
    }

    public async Task<Company?> GetCurrentCompanyAsync()
    {
        var companyId = CurrentCompanyId;
        if (companyId == null)
        {
            var companies = await GetUserCompaniesAsync();
            var defaultCompany = companies.FirstOrDefault(c => c.IsDefault) ?? companies.FirstOrDefault();
            if (defaultCompany != null)
                await SetCurrentCompanyAsync(defaultCompany.Id);
            companyId = defaultCompany?.Id;
        }
        return companyId.HasValue
            ? await _context.Companies.Include(c => c.Province).FirstOrDefaultAsync(c => c.Id == companyId)
            : null;
    }
}

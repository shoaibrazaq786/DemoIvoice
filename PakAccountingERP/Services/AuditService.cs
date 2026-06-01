using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;
using PakAccountingERP.Models;
using System.Security.Claims;

namespace PakAccountingERP.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string? tableName = null, string? recordId = null,
        string? oldValue = null, string? newValue = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var user = httpContext?.User;

        var log = new AuditLog
        {
            UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = user?.Identity?.Name,
            CompanyId = httpContext?.Session.GetInt32("CurrentCompanyId"),
            Action = action,
            TableName = tableName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IPAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            ActionDate = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(int? companyId = null, int take = 100)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (companyId.HasValue)
            query = query.Where(l => l.CompanyId == companyId);
        return await query.OrderByDescending(l => l.ActionDate).Take(take).ToListAsync();
    }
}

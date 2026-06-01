using Microsoft.EntityFrameworkCore;
using PakAccountingERP.Data;
using PakAccountingERP.Interfaces;

namespace PakAccountingERP.Services;

public class DocumentNumberService : IDocumentNumberService
{
    private readonly ApplicationDbContext _context;

    public DocumentNumberService(ApplicationDbContext context) => _context = context;

    public async Task<string> GetNextNumberAsync(int companyId, string documentType)
    {
        var sequence = await _context.DocumentSequences
            .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.DocumentType == documentType);

        if (sequence == null)
        {
            sequence = new Models.DocumentSequence
            {
                CompanyId = companyId,
                DocumentType = documentType,
                Prefix = documentType[..1].ToUpper(),
                LastNumber = 0,
                Padding = 6
            };
            _context.DocumentSequences.Add(sequence);
        }

        sequence.LastNumber++;
        await _context.SaveChangesAsync();
        return $"{sequence.Prefix}{sequence.LastNumber.ToString().PadLeft(sequence.Padding, '0')}";
    }
}

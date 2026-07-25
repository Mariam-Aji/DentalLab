using DentalLab.Api.Data;
using DentalLab.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Services;

public class LabMyFatoorahCodeService : ILabMyFatoorahCodeService
{
    private readonly ApplicationDbContext _db;

    public LabMyFatoorahCodeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<(LabMyFatoorahCodeResponseDto? result, string? error)> GetCodeAsync(int userId)
    {
        var lab = await _db.Labs
            .AsNoTracking()
            .Where(l => l.UserId == userId)
            .Select(l => new { l.MyFatoorahSupplierCode })
            .FirstOrDefaultAsync();

        if (lab == null)
            return (null, "Lab not found.");

        return (new LabMyFatoorahCodeResponseDto
        {
            MyFatoorahSupplierCode = lab.MyFatoorahSupplierCode
        }, null);
    }

    public async Task<(LabMyFatoorahCodeResponseDto? result, string? error)> UpdateCodeAsync(int userId, UpdateLabMyFatoorahCodeDto dto)
    {
        var lab = await _db.Labs
            .FirstOrDefaultAsync(l => l.UserId == userId);

        if (lab == null)
            return (null, "Lab not found.");

        lab.MyFatoorahSupplierCode = dto.MyFatoorahSupplierCode;

        await _db.SaveChangesAsync();

        return (new LabMyFatoorahCodeResponseDto
        {
            MyFatoorahSupplierCode = lab.MyFatoorahSupplierCode
        }, null);
    }
}

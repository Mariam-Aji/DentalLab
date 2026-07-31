using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Api.Repositories
{
    public class FileResourceRepository : IFileResourceRepository
    {
        private readonly ApplicationDbContext _context;

        public FileResourceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(FileResource file)
        {
            await _context.FileResources.AddAsync(file);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        //
        public async Task<List<FileResource>> GetStlFilesByCaseOrderIdAsync(int caseOrderId)
        {
            return await _context.FileResources
                .AsNoTracking()
                .Where(f => f.CaseOrderId == caseOrderId && f.Type == FileType.DigitalScan)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }
    }
}
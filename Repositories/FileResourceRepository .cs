using DentalLab.Api.Data;
using DentalLab.Api.Models;

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
    }
}
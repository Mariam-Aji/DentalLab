using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories
{
    public interface IFileResourceRepository
    {
        Task AddAsync(FileResource file);
        Task SaveChangesAsync();
        //
        Task<List<FileResource>> GetStlFilesByCaseOrderIdAsync(int caseOrderId);
    }
}
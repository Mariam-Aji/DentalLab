using System.Collections.Generic;
using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Repositories.Interfaces
{
    public interface IComplaintRepository
    {
        Task AddAsync(Complaint complaint);
        Task SaveChangesAsync();
        Task<List<Complaint>> GetAdminComplaintsAsync();
        Task<List<Complaint>> GetLabComplaintsAsync();
        Task<List<Complaint>> GetDentistComplaintsAsync(int dentistId);
        Task<Complaint?> GetByIdAsync(int id);
    }
}
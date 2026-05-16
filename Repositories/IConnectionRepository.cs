using DentalLab.Api.Models;

public interface IConnectionRepository
{
    Task<bool> CreateRequestAsync(ConnectionRequest request);
    Task<bool> RequestExistsAsync(int dentistId, int labId);
    Task<bool> LabExistsAsync(int labId); 
    Task<bool> DeleteRequestAsync(int dentistId, int labId); 
}
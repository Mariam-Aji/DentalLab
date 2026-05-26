using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

public interface ILabRepository
{
    Task<IEnumerable<Lab>> GetAllLabsWithOwnersAsync();
  
    Task<IEnumerable<Lab>> GetLabsByAvailabilityAsync(AvailabilityStatus status);

}

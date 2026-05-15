using DentalLab.Api.Data;
using DentalLab.Api.Models;
using Microsoft.EntityFrameworkCore;

public interface ILabRepository
{
    Task<IEnumerable<Lab>> GetAllLabsWithOwnersAsync();
    // توابع التصفية
    Task<IEnumerable<Lab>> GetLabsByAvailabilityAsync(AvailabilityStatus status);

}

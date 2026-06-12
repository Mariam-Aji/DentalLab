using System.Threading.Tasks;
using DentalLab.Api.Models;

namespace DentalLab.Api.Services;

public interface IUserService
{
    Task<(object? Data, string? Error)> SearchUsersServiceAsync(string searchTerm);
    Task<(object? Data, string? Error)> GetAllDentistsServiceAsync();
}
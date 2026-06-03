using System.Threading.Tasks;

namespace DentalLab.Api.Services;

public interface IPostService
{
    Task<bool> DeletePostAsync(int postId);
}
public interface IConnectionService
{
    Task<string> SendFollowRequestAsync(int dentistId, string userRole, int labId);
}
//
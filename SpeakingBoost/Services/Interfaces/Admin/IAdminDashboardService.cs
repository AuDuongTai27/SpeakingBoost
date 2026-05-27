using SpeakingBoost.Models.DTOs.Admin;

namespace SpeakingBoost.Services.Interfaces.Admin
{
    public interface IAdminDashboardService
    {
        Task<AdminDashboardDto> GetDashboardDataAsync(int? classId);
    }
}

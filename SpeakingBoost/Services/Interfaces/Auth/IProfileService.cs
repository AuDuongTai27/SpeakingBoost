using SpeakingBoost.Models.DTOs.Auth;

namespace SpeakingBoost.Services.Interfaces.Auth
{
    public interface IProfileService
    {
        Task<UserProfileDto?> GetProfileAsync(int userId);
        Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    }
}

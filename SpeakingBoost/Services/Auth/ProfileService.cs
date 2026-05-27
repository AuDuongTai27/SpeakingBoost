using SpeakingBoost.Models.DTOs.Auth;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Services.Auth
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginServices _loginServices;

        public ProfileService(IUserRepository userRepository, ILoginServices loginServices)
        {
            _userRepository = userRepository;
            _loginServices = loginServices;
        }

        public async Task<UserProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return null;

            return new UserProfileDto
            {
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
                return false;

            user.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = _loginServices.HashPassword(request.Password);
            }

            await _userRepository.UpdateUserAsync(user);
            return true;
        }
    }
}

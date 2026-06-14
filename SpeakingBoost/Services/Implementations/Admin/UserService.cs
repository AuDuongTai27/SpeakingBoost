using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Auth;
using SpeakingBoost.Services.Implementations.Auth;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILoginServices _loginServices;

        public UserService(IUserRepository userRepository, ILoginServices loginServices)
        {
            _userRepository = userRepository;
            _loginServices = loginServices;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllStudentsAsync();
            return users.Select(u => new UserDto
            {
                UserId   = u.UserId,
                FullName = u.FullName,
                Email    = u.Email,
                Role     = u.UserRoles.FirstOrDefault()?.Role?.RoleName ?? "user"
            }).ToList();
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            var userRole = user?.UserRoles.FirstOrDefault()?.Role?.RoleName;
            if (user == null || userRole != "user")
            {
                throw new KeyNotFoundException("Không tìm thấy học sinh.");
            }

            return new UserDto
            {
                UserId   = user.UserId,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = userRole
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
        {
            var normalizedEmail = dto.Email.ToLower().Trim();
            if (await _userRepository.EmailExistsAsync(normalizedEmail))
            {
                throw new InvalidOperationException("Email này đã được sử dụng.");
            }

            var user = new User
            {
                FullName     = dto.FullName,
                Email        = normalizedEmail,
                PasswordHash = _loginServices.HashPassword(dto.Password)
            };

            await _userRepository.AddUserAsync(user);
            // Gán role "user" qua bảng UserRoles
            await _userRepository.AddUserRoleAsync(user.UserId, roleName: "user");

            return new UserDto
            {
                UserId   = user.UserId,
                FullName = user.FullName,
                Email    = user.Email,
                Role     = "user"
            };
        }

        public async Task UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userRepository.GetUserByIdAsync(id);
            var userRole = user?.UserRoles.FirstOrDefault()?.Role?.RoleName;
            if (user == null || userRole != "user")
            {
                throw new KeyNotFoundException("Không tìm thấy học sinh.");
            }

            var normalizedEmail = dto.Email.ToLower().Trim();
            if (await _userRepository.EmailExistsExceptIdAsync(normalizedEmail, id))
            {
                throw new InvalidOperationException("Email này đã được sử dụng.");
            }

            user.FullName = dto.FullName;
            user.Email    = normalizedEmail;

            await _userRepository.UpdateUserAsync(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetUserWithRelationsByIdAsync(id);
            var userRole = user?.UserRoles.FirstOrDefault()?.Role?.RoleName;
            if (user == null || userRole != "user")
            {
                throw new KeyNotFoundException("Không tìm thấy học sinh.");
            }

            await _userRepository.DeleteUserRelationsAsync(user);
            await _userRepository.DeleteUserAsync(user);
        }
    }
}

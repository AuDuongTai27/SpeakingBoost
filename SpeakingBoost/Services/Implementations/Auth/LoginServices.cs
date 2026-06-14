using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using System.Security.Cryptography;
using System.Text;
using SpeakingBoost.Services.Interfaces.Auth;

namespace SpeakingBoost.Services.Implementations.Auth
{
    public class LoginServices : ILoginServices
    {
        private readonly ApplicationDbContext _context;

        public LoginServices(ApplicationDbContext context)
        {
            _context = context;
        }

        public User Login(string username, string password)
        {
            try
            {
                string hashedPassword = HashPassword(password);
                return _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .SingleOrDefault(t =>
                        t.Email == username.ToLower().Trim() &&
                        t.PasswordHash == hashedPassword.ToLower().Trim());
            }
            catch
            {
                return null;
            }
        }

        public User GetUserByEmail(string email)
        {
            try
            {
                return _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .SingleOrDefault(u =>
                        u.Email == email.ToLower().Trim());
            }
            catch
            {
                return null;
            }
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public bool UpdatePassword(int userId, string passwordHash)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.PasswordHash = passwordHash;
                _context.SaveChanges();
                return true;
            }
            return false;
        }
    }
}

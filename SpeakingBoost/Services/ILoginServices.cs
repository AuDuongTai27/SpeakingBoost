using SpeakingBoost.Models.Entities;

namespace SpeakingBoost.Services
{
    public interface ILoginServices
    {
        User Login(string username, string password);
        User GetUserByEmail(string email);
        bool UpdatePassword(int userId, string passwordHash);
        string HashPassword(string password);
    }
}
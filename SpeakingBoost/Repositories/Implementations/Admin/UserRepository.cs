using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Repositories.Implementations.Admin
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllStudentsAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "user"))
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> EmailExistsExceptIdAsync(string email, int id)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.UserId != id);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetUserWithRelationsByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.StudentClasses)
                .Include(u => u.Notifications)
                .Include(u => u.Submissions)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task DeleteUserRelationsAsync(User user)
        {
            // Xóa UserRoles trước
            var userRoles = _context.UserRoles.Where(ur => ur.UserId == user.UserId);
            _context.UserRoles.RemoveRange(userRoles);

            if (user.StudentClasses?.Any() == true)
            {
                _context.StudentClasses.RemoveRange(user.StudentClasses);
            }
            if (user.Notifications?.Any() == true)
            {
                _context.Notifications.RemoveRange(user.Notifications);
            }
            if (user.Submissions?.Any() == true)
            {
                _context.Submissions.RemoveRange(user.Submissions);
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetStudentsWithSubmissionsAndClassesAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == "user"))
                .Include(u => u.Submissions)
                .Include(u => u.StudentClasses)
                .ToListAsync();
        }

        public async Task<List<ClassExercise>> GetClassExercisesWithDeadlinesAsync(List<int> classIds)
        {
            return await _context.ClassExercises
                .Where(ce => classIds.Contains(ce.ClassId) && ce.Deadline.HasValue)
                .ToListAsync();
        }

        public async Task<User?> GetStudentWithSubmissionsAndScoresAsync(int studentId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Exercise)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Scores)
                .FirstOrDefaultAsync(u => u.UserId == studentId
                    && u.UserRoles.Any(ur => ur.Role.RoleName == "user"));
        }

        public async Task<List<Submission>> GetSubmissionsWithScoresAsync(int studentId, int exerciseId)
        {
            return await _context.Submissions
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId && s.ExerciseId == exerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Submission?> GetSubmissionWithExerciseAndScoresAsync(int submissionId)
        {
            return await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Student)
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
        }

        public async Task AddUserRoleAsync(int userId, string roleName)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName)
                       ?? throw new InvalidOperationException($"Role '{roleName}' not found in database.");

            var userRole = new UserRole { UserId = userId, RoleId = role.RoleId };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();
        }
    }
}

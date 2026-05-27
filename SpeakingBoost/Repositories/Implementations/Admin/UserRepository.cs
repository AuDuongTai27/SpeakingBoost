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
                .Where(u => u.Role == "user")
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
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
                .Include(u => u.StudentClasses)
                .Include(u => u.Notifications)
                .Include(u => u.Submissions)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task DeleteUserRelationsAsync(User user)
        {
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
                .Where(u => u.Role == "user")
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
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Exercise)
                .Include(u => u.Submissions)
                    .ThenInclude(s => s.Scores)
                .FirstOrDefaultAsync(u => u.UserId == studentId && u.Role == "user");
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
    }
}

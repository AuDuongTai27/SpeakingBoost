using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Repositories.Implementations.Admin
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SchoolClass>> GetClassesSortedByNameAsync()
        {
            return await _context.Classes
                .Include(c => c.StudentClasses)
                .OrderBy(c => c.ClassName)
                .ToListAsync();
        }

        public async Task<SchoolClass?> GetClassWithStudentClassesAsync(int classId)
        {
            return await _context.Classes
                .Include(c => c.StudentClasses)
                .FirstOrDefaultAsync(c => c.ClassId == classId);
        }

        public async Task<List<Submission>> GetSubmissionsByStudentIdsAsync(List<int> studentIds)
        {
            return await _context.Submissions
                .Where(s => studentIds.Contains(s.StudentId))
                .Include(s => s.Scores)
                .Include(s => s.Student)
                .Include(s => s.Exercise)
                .ToListAsync();
        }

        public async Task<int> CountClassExercisesWithDeadlinesAsync(int classId)
        {
            return await _context.ClassExercises
                .Where(ce => ce.ClassId == classId && ce.Deadline.HasValue)
                .CountAsync();
        }
    }
}

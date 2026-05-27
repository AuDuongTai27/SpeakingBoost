using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;

namespace SpeakingBoost.Repositories.Implementations.Student
{
    public class StudentDashboardRepository : IStudentDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<int>> GetClassIdsByStudentAsync(int studentId)
        {
            return await _context.StudentClasses
                .Where(sc => sc.StudentId == studentId)
                .Select(sc => sc.ClassId)
                .ToListAsync();
        }

        public async Task<List<ClassExercise>> GetAssignedExercisesAsync(List<int> classIds)
        {
            return await _context.ClassExercises
                .Include(ce => ce.Exercise)
                .Include(ce => ce.SchoolClass)
                .Where(ce => classIds.Contains(ce.ClassId))
                .ToListAsync();
        }

        public async Task<List<Submission>> GetStudentSubmissionsWithScoresAsync(int studentId)
        {
            return await _context.Submissions
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId)
                .ToListAsync();
        }
    }
}

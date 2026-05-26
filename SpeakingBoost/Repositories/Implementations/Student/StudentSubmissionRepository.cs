using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;

namespace SpeakingBoost.Repositories.Implementations.Student
{
    public class StudentSubmissionRepository : IStudentSubmissionRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentSubmissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Submission>> GetAllByStudentAsync(int studentId)
        {
            return await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Submission>> GetPracticeHistoryAsync(int studentId, int exerciseId)
        {
            return await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId
                         && s.ExerciseId == exerciseId
                         && s.ClassExerciseId == null)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Submission>> GetDeadlineHistoryAsync(int studentId, int classExerciseId)
        {
            return await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId && s.ClassExerciseId == classExerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<Submission?> GetDetailAsync(int submissionId, int studentId)
        {
            return await _context.Submissions
                .Include(s => s.Exercise)
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId && s.StudentId == studentId);
        }

        public async Task<Submission?> GetStatusAsync(int submissionId, int studentId)
        {
            return await _context.Submissions
                .Include(s => s.Scores)
                .FirstOrDefaultAsync(s => s.SubmissionId == submissionId && s.StudentId == studentId);
        }
    }
}

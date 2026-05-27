using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;

namespace SpeakingBoost.Repositories.Implementations.Student
{
    public class StudentDeadlineRepository : IStudentDeadlineRepository
    {
        private readonly ApplicationDbContext _context;

        public StudentDeadlineRepository(ApplicationDbContext context)
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

        public async Task<List<ClassExercise>> GetDeadlinesByClassIdsAsync(List<int> classIds)
        {
            return await _context.ClassExercises
                .Include(ce => ce.Exercise)
                .Include(ce => ce.SchoolClass)
                .Where(ce => classIds.Contains(ce.ClassId) && ce.Deadline.HasValue)
                .OrderBy(ce => ce.Deadline)
                .ToListAsync();
        }

        public async Task<List<Submission>> GetDeadlineSubmissionsAsync(int studentId)
        {
            return await _context.Submissions
                .Where(s => s.StudentId == studentId && s.ClassExerciseId != null)
                .Include(s => s.Scores)
                .ToListAsync();
        }

        public async Task<ClassExercise?> GetClassExerciseWithDetailsAsync(int classExerciseId)
        {
            return await _context.ClassExercises
                .Include(x => x.Exercise)
                .Include(x => x.SchoolClass)
                .FirstOrDefaultAsync(x => x.ClassExerciseId == classExerciseId);
        }

        public async Task<bool> IsStudentInClassAsync(int studentId, int classId)
        {
            return await _context.StudentClasses
                .AnyAsync(sc => sc.StudentId == studentId && sc.ClassId == classId);
        }

        public async Task<int> CountAttemptsByClassExerciseAsync(int studentId, int classExerciseId)
        {
            return await _context.Submissions
                .CountAsync(s => s.StudentId == studentId && s.ClassExerciseId == classExerciseId);
        }

        public async Task<Submission?> GetLatestDeadlineSubmissionAsync(int studentId, int classExerciseId)
        {
            return await _context.Submissions
                .Include(s => s.Scores)
                .Where(s => s.StudentId == studentId && s.ClassExerciseId == classExerciseId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Submission> AddSubmissionAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }
    }
}

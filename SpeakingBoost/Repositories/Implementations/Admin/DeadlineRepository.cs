using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Repositories.Implementations.Admin
{
    public class DeadlineRepository : IDeadlineRepository
    {
        private readonly ApplicationDbContext _context;

        public DeadlineRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ClassExercise>> GetActiveDeadlinesAsync()
        {
            return await _context.ClassExercises
                .Include(ce => ce.SchoolClass)
                .Include(ce => ce.Exercise)
                    .ThenInclude(e => e.VocabularyTopic)
                .Where(ce => ce.Deadline.HasValue)
                .OrderByDescending(ce => ce.Deadline)
                .ToListAsync();
        }

        public async Task<List<SchoolClass>> GetClassesSortedAsync()
        {
            return await _context.Classes
                .OrderBy(c => c.ClassName)
                .ToListAsync();
        }

        public async Task<List<VocabularyTopic>> GetTopicsSortedAsync()
        {
            return await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<List<Exercise>> GetExercisesByTopicIdAsync(int topicId)
        {
            return await _context.Exercises
                .Where(e => e.TopicId == topicId)
                .ToListAsync();
        }

        public async Task<SchoolClass?> GetClassByIdAsync(int classId)
        {
            return await _context.Classes.FindAsync(classId);
        }

        public async Task<VocabularyTopic?> GetTopicByIdAsync(int topicId)
        {
            return await _context.VocabularyTopics.FindAsync(topicId);
        }

        public async Task<ClassExercise?> GetClassExerciseAsync(int classId, int exerciseId)
        {
            return await _context.ClassExercises
                .FirstOrDefaultAsync(ce => ce.ClassId == classId && ce.ExerciseId == exerciseId);
        }

        public async Task AddClassExerciseAsync(ClassExercise classExercise)
        {
            _context.ClassExercises.Add(classExercise);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClassExerciseAsync(ClassExercise classExercise)
        {
            _context.ClassExercises.Update(classExercise);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassExercise?> GetClassExerciseByIdAsync(int id)
        {
            return await _context.ClassExercises.FindAsync(id);
        }

        public async Task DeleteClassExerciseAsync(ClassExercise classExercise)
        {
            _context.ClassExercises.Remove(classExercise);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ClassExercise>> GetClassExercisesAsync(int classId, List<int> exerciseIds)
        {
            return await _context.ClassExercises
                .Where(ce => ce.ClassId == classId && exerciseIds.Contains(ce.ExerciseId))
                .ToListAsync();
        }

        public async Task DeleteClassExercisesRangeAsync(List<ClassExercise> assignments)
        {
            _context.ClassExercises.RemoveRange(assignments);
            await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetStudentsByClassIdAsync(int classId)
        {
            return await _context.StudentClasses
                .Include(sc => sc.Student)
                .Where(sc => sc.ClassId == classId && sc.Student.Role == "user")
                .Select(sc => sc.Student)
                .ToListAsync();
        }
    }
}

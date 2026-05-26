using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Repositories.Implementations.Admin
{
    public class ClassRepository : IClassRepository
    {
        private readonly ApplicationDbContext _context;

        public ClassRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SchoolClass>> GetAllClassesAsync()
        {
            return await _context.Classes
                .Include(c => c.StudentClasses)
                .OrderBy(c => c.ClassName)
                .ToListAsync();
        }

        public async Task<SchoolClass?> GetClassByIdAsync(int id)
        {
            return await _context.Classes
                .Include(x => x.StudentClasses)
                .FirstOrDefaultAsync(x => x.ClassId == id);
        }

        public async Task<bool> ClassNameExistsAsync(string className)
        {
            return await _context.Classes.AnyAsync(c => c.ClassName == className);
        }

        public async Task<bool> ClassNameExistsExceptIdAsync(string className, int id)
        {
            return await _context.Classes.AnyAsync(c => c.ClassName == className && c.ClassId != id);
        }

        public async Task AddClassAsync(SchoolClass schoolClass)
        {
            _context.Classes.Add(schoolClass);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateClassAsync(SchoolClass schoolClass)
        {
            _context.Classes.Update(schoolClass);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteClassAsync(SchoolClass schoolClass)
        {
            _context.Classes.Remove(schoolClass);
            await _context.SaveChangesAsync();
        }

        public async Task<SchoolClass?> GetClassWithStudentsAndExercisesAsync(int id)
        {
            return await _context.Classes
                .Include(c => c.StudentClasses)
                    .ThenInclude(sc => sc.Student)
                .FirstOrDefaultAsync(c => c.ClassId == id);
        }

        public async Task<Dictionary<int, int>> GetSubmissionCountsByStudentIdsAsync(List<int> studentIds)
        {
            var list = await _context.Submissions
                .Where(s => studentIds.Contains(s.StudentId))
                .GroupBy(s => s.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToListAsync();

            return list.ToDictionary(x => x.StudentId, x => x.Count);
        }

        public async Task<bool> IsStudentInClassAsync(int classId, int studentId)
        {
            return await _context.StudentClasses
                .AnyAsync(sc => sc.ClassId == classId && sc.StudentId == studentId);
        }

        public async Task AddStudentToClassAsync(StudentClass studentClass)
        {
            _context.StudentClasses.Add(studentClass);
            await _context.SaveChangesAsync();
        }

        public async Task<StudentClass?> GetStudentClassByIdAsync(int studentClassId)
        {
            return await _context.StudentClasses.FindAsync(studentClassId);
        }

        public async Task RemoveStudentFromClassAsync(StudentClass record)
        {
            _context.StudentClasses.Remove(record);
            await _context.SaveChangesAsync();
        }

        public async Task<ClassExercise?> GetClassExerciseByIdAsync(int classExerciseId)
        {
            return await _context.ClassExercises.FindAsync(classExerciseId);
        }

        public async Task UpdateClassExerciseAsync(ClassExercise assignment)
        {
            _context.ClassExercises.Update(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ClassExercise>> GetAssignedExercisesByClassIdAsync(int classId)
        {
            return await _context.ClassExercises
                .Include(ce => ce.Exercise)
                    .ThenInclude(e => e.VocabularyTopic)
                .Where(ce => ce.ClassId == classId)
                .OrderBy(ce => ce.Deadline)
                .ToListAsync();
        }
    }
}

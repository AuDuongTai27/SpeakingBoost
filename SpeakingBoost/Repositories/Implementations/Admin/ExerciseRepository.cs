using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;

namespace SpeakingBoost.Repositories.Implementations.Admin
{
    public class ExerciseRepository : IExerciseRepository
    {
        private readonly ApplicationDbContext _context;

        public ExerciseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VocabularyTopic>> GetAllTopicsAsync()
        {
            return await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .OrderByDescending(t => t.TopicId)
                .ToListAsync();
        }

        public async Task<VocabularyTopic?> GetTopicByIdAsync(int id)
        {
            return await _context.VocabularyTopics.FindAsync(id);
        }

        public async Task<bool> TopicNameExistsAsync(string name)
        {
            return await _context.VocabularyTopics
                .AnyAsync(t => t.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> TopicNameExistsExceptIdAsync(string name, int id)
        {
            return await _context.VocabularyTopics
                .AnyAsync(t => t.Name.ToLower() == name.ToLower() && t.TopicId != id);
        }

        public async Task AddTopicAsync(VocabularyTopic topic)
        {
            _context.VocabularyTopics.Add(topic);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTopicAsync(VocabularyTopic topic)
        {
            _context.VocabularyTopics.Update(topic);
            await _context.SaveChangesAsync();
        }

        public async Task<VocabularyTopic?> GetTopicWithExercisesAsync(int id)
        {
            return await _context.VocabularyTopics
                .Include(t => t.Exercises)
                .FirstOrDefaultAsync(t => t.TopicId == id);
        }

        public async Task<bool> HasSubmissionsForExercisesAsync(List<int> exerciseIds)
        {
            return await _context.Submissions
                .AnyAsync(s => exerciseIds.Contains(s.ExerciseId));
        }

        public async Task DeleteExercisesRangeAsync(List<Exercise> exercises)
        {
            _context.Exercises.RemoveRange(exercises);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTopicAsync(VocabularyTopic topic)
        {
            _context.VocabularyTopics.Remove(topic);
            await _context.SaveChangesAsync();
        }

        public async Task AddExerciseAsync(Exercise exercise)
        {
            _context.Exercises.Add(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task<Exercise?> GetExerciseByIdAsync(int id)
        {
            return await _context.Exercises.FindAsync(id);
        }

        public async Task<Exercise?> GetExerciseWithTopicByIdAsync(int id)
        {
            return await _context.Exercises
                .Include(e => e.VocabularyTopic)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);
        }

        public async Task UpdateExerciseAsync(Exercise exercise)
        {
            _context.Exercises.Update(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task<Exercise?> GetExerciseWithSubmissionsByIdAsync(int id)
        {
            return await _context.Exercises
                .Include(e => e.Submissions)
                .Include(e => e.ClassExercises)
                .FirstOrDefaultAsync(e => e.ExerciseId == id);
        }

        public async Task DeleteSubmissionsRangeAsync(List<Submission> submissions)
        {
            _context.Submissions.RemoveRange(submissions);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteExerciseAsync(Exercise exercise)
        {
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
        }

        public async Task AddExercisesRangeAsync(List<Exercise> exercises)
        {
            _context.Exercises.AddRange(exercises);
            await _context.SaveChangesAsync();
        }
    }
}

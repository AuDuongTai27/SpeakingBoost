using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Student;

namespace SpeakingBoost.Repositories.Implementations.Student
{
    public class PracticeRepository : IPracticeRepository
    {
        private readonly ApplicationDbContext _context;

        public PracticeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<(int TopicId, string Name, string? Description, int QuestionCount)>> GetTopicsWithCountAsync(int part)
        {
            var partKey = $"part{part}";

            var topics = await _context.VocabularyTopics
                .AsNoTracking()
                .Select(t => new
                {
                    t.TopicId,
                    t.Name,
                    t.Description,
                    QuestionCount = t.Exercises!.Count(e =>
                        part == 0 || e.Type.ToLower() == partKey)
                })
                .Where(t => t.QuestionCount > 0)
                .ToListAsync();

            return topics
                .Select(t => (t.TopicId, t.Name, t.Description, t.QuestionCount))
                .ToList();
        }

        public async Task<(int TopicId, string Name)?> GetTopicHeaderAsync(int topicId)
        {
            var topic = await _context.VocabularyTopics
                .AsNoTracking()
                .Where(t => t.TopicId == topicId)
                .Select(t => new { t.TopicId, t.Name })
                .FirstOrDefaultAsync();

            if (topic == null) return null;
            return (topic.TopicId, topic.Name);
        }

        public async Task<List<(Exercise Exercise, int AttemptUsed)>> GetTopicQuestionsWithAttemptsAsync(int topicId, int part, int studentId)
        {
            var partKey = $"part{part}";

            var results = await (
                from e in _context.Exercises.AsNoTracking()
                where e.TopicId == topicId
                      && (part == 0 || e.Type.ToLower() == partKey)
                join s in _context.Submissions.AsNoTracking()
                    .Where(x => x.StudentId == studentId)
                    on e.ExerciseId equals s.ExerciseId into sg
                orderby e.ExerciseId
                select new
                {
                    Exercise = e,
                    AttemptUsed = sg.Count()
                }
            ).ToListAsync();

            return results.Select(r => (r.Exercise, r.AttemptUsed)).ToList();
        }

        public async Task<Submission> AddSubmissionAsync(Submission submission)
        {
            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task UpdateSubmissionAsync(Submission submission)
        {
            _context.Submissions.Update(submission);
            await _context.SaveChangesAsync();
        }
    }
}

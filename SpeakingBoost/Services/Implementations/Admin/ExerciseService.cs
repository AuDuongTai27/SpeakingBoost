using ClosedXML.Excel;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class ExerciseService : IExerciseService
    {
        private readonly IExerciseRepository _exerciseRepository;

        public ExerciseService(IExerciseRepository exerciseRepository)
        {
            _exerciseRepository = exerciseRepository;
        }

        public async Task<List<TopicDto>> GetAllTopicsAsync()
        {
            var topics = await _exerciseRepository.GetAllTopicsAsync();
            return topics.Select(t => new TopicDto
            {
                TopicId       = t.TopicId,
                Name          = t.Name,
                Description   = t.Description,
                ExerciseCount = t.Exercises?.Count ?? 0
            }).ToList();
        }

        public async Task<TopicDto> CreateTopicAsync(CreateTopicDto dto)
        {
            if (await _exerciseRepository.TopicNameExistsAsync(dto.Name))
            {
                throw new InvalidOperationException("Chủ đề này đã tồn tại.");
            }

            var topic = new VocabularyTopic
            {
                Name        = dto.Name,
                Description = dto.Description
            };

            await _exerciseRepository.AddTopicAsync(topic);

            return new TopicDto
            {
                TopicId     = topic.TopicId,
                Name        = topic.Name,
                Description = topic.Description
            };
        }

        public async Task UpdateTopicAsync(int id, CreateTopicDto dto)
        {
            var topic = await _exerciseRepository.GetTopicByIdAsync(id);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            if (await _exerciseRepository.TopicNameExistsExceptIdAsync(dto.Name, id))
            {
                throw new InvalidOperationException("Tên chủ đề này đã tồn tại.");
            }

            topic.Name        = dto.Name;
            topic.Description = dto.Description;

            await _exerciseRepository.UpdateTopicAsync(topic);
        }

        public async Task DeleteTopicAsync(int id)
        {
            var topic = await _exerciseRepository.GetTopicWithExercisesAsync(id);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            var exerciseIds = topic.Exercises?.Select(e => e.ExerciseId).ToList() ?? new List<int>();
            if (exerciseIds.Any())
            {
                var hasSubmissions = await _exerciseRepository.HasSubmissionsForExercisesAsync(exerciseIds);
                if (hasSubmissions)
                {
                    throw new InvalidOperationException("Không thể xóa chủ đề này vì đã có học viên nộp bài.");
                }

                await _exerciseRepository.DeleteExercisesRangeAsync(topic.Exercises.ToList());
            }

            await _exerciseRepository.DeleteTopicAsync(topic);
        }

        public async Task<TopicDetailsDto> GetTopicDetailsAsync(int id)
        {
            var topic = await _exerciseRepository.GetTopicWithExercisesAsync(id);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            return new TopicDetailsDto
            {
                TopicId     = topic.TopicId,
                Name        = topic.Name,
                Description = topic.Description,
                Exercises   = topic.Exercises?.Select(e => new ExerciseDto
                {
                    ExerciseId   = e.ExerciseId,
                    Title        = e.Title,
                    Type         = e.Type,
                    Question     = e.Question,
                    SampleAnswer = e.SampleAnswer,
                    MaxAttempts  = e.MaxAttempts,
                    TopicId      = e.TopicId
                }).ToList() ?? new List<ExerciseDto>()
            };
        }

        public async Task<ExerciseDto> AddExerciseAsync(int topicId, CreateExerciseDto dto)
        {
            var topic = await _exerciseRepository.GetTopicByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            var exercise = new Exercise
            {
                Title        = dto.Title,
                Type         = dto.Type,
                Question     = dto.Question,
                SampleAnswer = dto.SampleAnswer,
                MaxAttempts  = dto.MaxAttempts,
                TopicId      = topicId
            };

            await _exerciseRepository.AddExerciseAsync(exercise);

            return new ExerciseDto
            {
                ExerciseId   = exercise.ExerciseId,
                Title        = exercise.Title,
                Type         = exercise.Type,
                Question     = exercise.Question,
                SampleAnswer = exercise.SampleAnswer,
                MaxAttempts  = exercise.MaxAttempts,
                TopicId      = exercise.TopicId
            };
        }

        public async Task<ExerciseDto> GetExerciseAsync(int id)
        {
            var exercise = await _exerciseRepository.GetExerciseWithTopicByIdAsync(id);
            if (exercise == null)
            {
                throw new KeyNotFoundException("Không tìm thấy câu hỏi.");
            }

            return new ExerciseDto
            {
                ExerciseId   = exercise.ExerciseId,
                Title        = exercise.Title,
                Type         = exercise.Type,
                Question     = exercise.Question,
                SampleAnswer = exercise.SampleAnswer,
                MaxAttempts  = exercise.MaxAttempts,
                TopicId      = exercise.TopicId,
                TopicName    = exercise.VocabularyTopic?.Name
            };
        }

        public async Task UpdateExerciseAsync(int id, UpdateExerciseDto dto)
        {
            var exercise = await _exerciseRepository.GetExerciseByIdAsync(id);
            if (exercise == null)
            {
                throw new KeyNotFoundException("Không tìm thấy câu hỏi.");
            }

            if (!dto.TopicId.HasValue)
            {
                throw new ArgumentException("Chủ đề là bắt buộc.");
            }

            var topic = await _exerciseRepository.GetTopicByIdAsync(dto.TopicId.Value);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            exercise.Title        = dto.Title;
            exercise.Type         = dto.Type;
            exercise.Question     = dto.Question;
            exercise.SampleAnswer = dto.SampleAnswer;
            exercise.MaxAttempts  = dto.MaxAttempts;
            exercise.TopicId      = dto.TopicId;

            await _exerciseRepository.UpdateExerciseAsync(exercise);
        }

        public async Task DeleteExerciseAsync(int id)
        {
            var exercise = await _exerciseRepository.GetExerciseWithSubmissionsByIdAsync(id);
            if (exercise == null)
            {
                throw new KeyNotFoundException("Không tìm thấy câu hỏi.");
            }

            if (exercise.Submissions != null && exercise.Submissions.Any())
            {
                await _exerciseRepository.DeleteSubmissionsRangeAsync(exercise.Submissions.ToList());
            }

            // Note: DB cascades or manual delete can also remove ClassExercises link
            if (exercise.ClassExercises != null && exercise.ClassExercises.Any())
            {
                // In context, EF Core handles standard relationship deletes or we can let EF Core handle it.
                // We'll let EF Core handle ClassExercises if cascading, or delete them if needed.
                // Since this was not manually deleted in original, let's keep it consistent.
            }

            await _exerciseRepository.DeleteExerciseAsync(exercise);
        }

        public async Task<int> ImportFromExcelAsync(int topicId, Stream excelStream)
        {
            var topic = await _exerciseRepository.GetTopicByIdAsync(topicId);
            if (topic == null)
            {
                throw new KeyNotFoundException("Không tìm thấy chủ đề.");
            }

            int successCount = 0;
            int currentRow = 2;

            using var workbook = new XLWorkbook(excelStream);
            var worksheet = workbook.Worksheet(1);
            var lastRow   = worksheet.LastRowUsed()?.RowNumber() ?? 1;

            var exercisesToAdd = new List<Exercise>();

            for (int row = 2; row <= lastRow; row++)
            {
                currentRow = row;
                var excelRow = worksheet.Row(row);

                var title        = excelRow.Cell(1).GetValue<string>()?.Trim();
                var partVal      = excelRow.Cell(2).GetValue<string>()?.Trim();
                var questionText = excelRow.Cell(3).GetValue<string>()?.Trim();
                var sampleAnswer = excelRow.Cell(4).GetValue<string>()?.Trim();
                var maxVal       = excelRow.Cell(5).GetValue<string>()?.Trim();

                if (string.IsNullOrEmpty(questionText)) continue;

                int.TryParse(partVal, out int partNumber);
                string typeString = partNumber switch { 2 => "Part2", 3 => "Part3", _ => "Part1" };

                if (!int.TryParse(maxVal, out int maxAttempts) || maxAttempts <= 0)
                {
                    maxAttempts = 3;
                }

                exercisesToAdd.Add(new Exercise
                {
                    TopicId      = topicId,
                    Title        = string.IsNullOrEmpty(title) ? $"Câu hỏi (Dòng {row})" : title,
                    Type         = typeString,
                    Question     = questionText,
                    SampleAnswer = sampleAnswer,
                    MaxAttempts  = maxAttempts
                });
                successCount++;
            }

            if (successCount > 0)
            {
                await _exerciseRepository.AddExercisesRangeAsync(exercisesToAdd);
            }
            else
            {
                throw new InvalidOperationException("File không có dữ liệu hợp lệ.");
            }

            return successCount;
        }
    }
}

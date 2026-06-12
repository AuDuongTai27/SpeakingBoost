using Moq;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Implementations.Admin;
using Xunit;

namespace SpeakingBoost.Tests.Services
{
    /// <summary>
    /// Unit Tests cho ExerciseService — dùng Moq để mock IExerciseRepository
    /// </summary>
    public class ExerciseServiceTests
    {
        private readonly Mock<IExerciseRepository> _mockRepo;
        private readonly ExerciseService _service;

        public ExerciseServiceTests()
        {
            _mockRepo = new Mock<IExerciseRepository>();
            _service  = new ExerciseService(_mockRepo.Object);
        }

        // ════════════════════════════════════════════════════
        // TOPIC TESTS
        // ════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────
        // TC-E01: Tạo topic mới thành công → trả về TopicDto
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task CreateTopicAsync_ValidDto_ReturnsTopicDto()
        {
            // Arrange
            var dto = new CreateTopicDto { Name = "IELTS Topic", Description = "Mô tả" };
            _mockRepo.Setup(r => r.TopicNameExistsAsync(dto.Name)).ReturnsAsync(false);
            _mockRepo.Setup(r => r.AddTopicAsync(It.IsAny<VocabularyTopic>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateTopicAsync(dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IELTS Topic", result.Name);
            _mockRepo.Verify(r => r.AddTopicAsync(It.IsAny<VocabularyTopic>()), Times.Once);
        }

        // ─────────────────────────────────────────────────────
        // TC-E02: Tạo topic trùng tên → ném InvalidOperationException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task CreateTopicAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new CreateTopicDto { Name = "Existing Topic" };
            _mockRepo.Setup(r => r.TopicNameExistsAsync(dto.Name)).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.CreateTopicAsync(dto));
            Assert.Contains("đã tồn tại", ex.Message);
        }

        // ─────────────────────────────────────────────────────
        // TC-E03: UpdateTopic với id không tồn tại → ném KeyNotFoundException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateTopicAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTopicByIdAsync(999)).ReturnsAsync((VocabularyTopic?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateTopicAsync(999, new CreateTopicDto { Name = "New" }));
        }

        // ─────────────────────────────────────────────────────
        // TC-E04: UpdateTopic trùng tên với topic khác → ném InvalidOperationException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateTopicAsync_DuplicateName_ThrowsInvalidOperationException()
        {
            // Arrange
            var existingTopic = new VocabularyTopic { TopicId = 1, Name = "Old Name" };
            var dto = new CreateTopicDto { Name = "Taken Name" };

            _mockRepo.Setup(r => r.GetTopicByIdAsync(1)).ReturnsAsync(existingTopic);
            _mockRepo.Setup(r => r.TopicNameExistsExceptIdAsync("Taken Name", 1)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateTopicAsync(1, dto));
        }

        // ─────────────────────────────────────────────────────
        // TC-E05: DeleteTopic khi topic không tồn tại → ném KeyNotFoundException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteTopicAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTopicWithExercisesAsync(999)).ReturnsAsync((VocabularyTopic?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteTopicAsync(999));
        }

        // ─────────────────────────────────────────────────────
        // TC-E06: DeleteTopic khi exercise đã có submission → không cho xóa
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteTopicAsync_WithSubmissions_ThrowsInvalidOperationException()
        {
            // Arrange
            var topic = new VocabularyTopic
            {
                TopicId   = 1,
                Name      = "Topic With Submissions",
                Exercises = new List<Exercise>
                {
                    new Exercise { ExerciseId = 10, Title = "Exercise 1" }
                }
            };
            _mockRepo.Setup(r => r.GetTopicWithExercisesAsync(1)).ReturnsAsync(topic);
            _mockRepo.Setup(r => r.HasSubmissionsForExercisesAsync(It.IsAny<List<int>>())).ReturnsAsync(true);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteTopicAsync(1));
            Assert.Contains("học viên nộp bài", ex.Message);
        }

        // ════════════════════════════════════════════════════
        // EXERCISE TESTS
        // ════════════════════════════════════════════════════

        // ─────────────────────────────────────────────────────
        // TC-E07: Thêm exercise thành công → trả về ExerciseDto
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task AddExerciseAsync_ValidData_ReturnsExerciseDto()
        {
            // Arrange
            var topic = new VocabularyTopic { TopicId = 1, Name = "Topic 1" };
            var dto = new CreateExerciseDto
            {
                Title       = "Describe your hometown",
                Type        = "Part1",
                Question    = "Where are you from?",
                MaxAttempts = 3
            };

            _mockRepo.Setup(r => r.GetTopicByIdAsync(1)).ReturnsAsync(topic);
            _mockRepo.Setup(r => r.AddExerciseAsync(It.IsAny<Exercise>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddExerciseAsync(1, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Describe your hometown", result.Title);
            Assert.Equal("Part1", result.Type);
            Assert.Equal(1, result.TopicId);
        }

        // ─────────────────────────────────────────────────────
        // TC-E08: AddExercise với topicId không tồn tại → lỗi
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task AddExerciseAsync_InvalidTopicId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetTopicByIdAsync(999)).ReturnsAsync((VocabularyTopic?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.AddExerciseAsync(999, new CreateExerciseDto { Title = "Test", Type = "Part1", Question = "Q?" }));
        }

        // ─────────────────────────────────────────────────────
        // TC-E09: UpdateExercise khi không có TopicId → ném ArgumentException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task UpdateExerciseAsync_MissingTopicId_ThrowsArgumentException()
        {
            // Arrange
            var exercise = new Exercise { ExerciseId = 1, Title = "Old Title" };
            var dto = new UpdateExerciseDto
            {
                Title    = "New Title",
                Type     = "Part2",
                Question = "New Q?",
                TopicId  = null  // ← không có TopicId
            };

            _mockRepo.Setup(r => r.GetExerciseByIdAsync(1)).ReturnsAsync(exercise);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.UpdateExerciseAsync(1, dto));
        }

        // ─────────────────────────────────────────────────────
        // TC-E10: GetExercise với id không tồn tại → ném KeyNotFoundException
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task GetExerciseAsync_NonexistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetExerciseWithTopicByIdAsync(999)).ReturnsAsync((Exercise?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetExerciseAsync(999));
        }

        // ─────────────────────────────────────────────────────
        // TC-E11: DeleteExercise có submission → xóa submission trước
        // ─────────────────────────────────────────────────────
        [Fact]
        public async Task DeleteExerciseAsync_WithSubmissions_DeletesSubmissionsFirst()
        {
            // Arrange
            var exercise = new Exercise
            {
                ExerciseId  = 1,
                Title       = "To Delete",
                Submissions = new List<Submission>
                {
                    new Submission { SubmissionId = 100, StudentId = 1, ExerciseId = 1 }
                }
            };

            _mockRepo.Setup(r => r.GetExerciseWithSubmissionsByIdAsync(1)).ReturnsAsync(exercise);
            _mockRepo.Setup(r => r.DeleteSubmissionsRangeAsync(It.IsAny<List<Submission>>())).Returns(Task.CompletedTask);
            _mockRepo.Setup(r => r.DeleteExerciseAsync(exercise)).Returns(Task.CompletedTask);

            // Act
            await _service.DeleteExerciseAsync(1);

            // Assert: phải xóa submissions trước
            _mockRepo.Verify(r => r.DeleteSubmissionsRangeAsync(It.IsAny<List<Submission>>()), Times.Once);
            _mockRepo.Verify(r => r.DeleteExerciseAsync(exercise), Times.Once);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Student;
using SpeakingBoost.Models.EF;

namespace SpeakingBoost.Controllers.Student
{
    [ApiController]
    [Route("api/student/vocabulary")]
    [Authorize(Roles = "student")]
    public class VocabularyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VocabularyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _context.VocabularyTopics
                .Select(t => new VocabularyTopicDto
                {
                    Id = t.TopicId,
                    Name = t.Name
                })
                .OrderBy(t => t.Name)
                .ToListAsync();

            return Ok(ApiResponse<List<VocabularyTopicDto>>.SuccessResponse(topics));
        }

        [HttpGet("topics/{id}")]
        public async Task<IActionResult> GetTopicDetails(int id)
        {
            var topic = await _context.VocabularyTopics.FindAsync(id);
            if (topic == null) return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            var words = await _context.Vocabulary
                .Where(w => w.TopicId == id)
                .Select(w => new WordDto
                {
                    Word = w.Word,
                    Meaning = w.Meaning,
                    Example = w.Example
                })
                .ToListAsync();

            var dto = new VocabularyDetailsDto
            {
                TopicName = topic.Name,
                Words = words
            };

            return Ok(ApiResponse<VocabularyDetailsDto>.SuccessResponse(dto));
        }
    }
}

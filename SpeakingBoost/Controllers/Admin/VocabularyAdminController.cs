using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpeakingBoost.Models.DTOs;
using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.EF;
using SpeakingBoost.Models.Entities;
using ClosedXML.Excel;

// ================================================================
// VocabularyAdminController — tương đương VocabularyAdminController (MVC)
//
// MVC cũ:                                           API mới:
// ────────────────────────────────────────────────  ──────────────────────────────────────────
// GET  /Admin/VocabularyAdmin/Index?topicId=5     →  GET    /api/admin/vocabulary?topicId=5
// POST /Admin/VocabularyAdmin/AddWord             →  POST   /api/admin/vocabulary
// POST /Admin/VocabularyAdmin/DeleteWord/5        →  DELETE /api/admin/vocabulary/5
// GET  /Admin/VocabularyAdmin/ViewTopics          →  GET    /api/admin/vocabulary/topics
// GET  /Admin/VocabularyAdmin/Details/5           →  GET    /api/admin/vocabulary/topics/5
// POST /Admin/VocabularyAdmin/ImportVocabulary    →  POST   /api/admin/vocabulary/import
// ================================================================

namespace SpeakingBoost.Controllers.Admin
{
    [ApiController]
    [Authorize(Roles = "teacher,superadmin")]
    public class VocabularyAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VocabularyAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/vocabulary?topicId=5
        // MVC cũ: Index(topicId?) — danh sách từ vựng + form thêm từ
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/vocabulary")]
        public async Task<IActionResult> GetVocabulary([FromQuery] int? topicId = null)
        {
            var query = _context.Vocabulary
                .Include(v => v.VocabularyTopic)
                .AsQueryable();

            if (topicId.HasValue)
                query = query.Where(v => v.TopicId == topicId);

            var words = await query
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VocabWordDto
                {
                    VocabId   = v.VocabId,
                    Word      = v.Word,
                    Meaning   = v.Meaning,
                    Example   = v.Example,
                    Note      = v.Note,
                    TopicId   = v.TopicId,
                    TopicName = v.VocabularyTopic != null ? v.VocabularyTopic.Name : null,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            // Kèm danh sách topics cho dropdown
            var topics = await _context.VocabularyTopics
                .OrderBy(t => t.Name)
                .Select(t => new { t.TopicId, t.Name })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                Words  = words,
                Topics = topics
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/vocabulary
        // Body: CreateVocabDto
        // MVC cũ: AddWord(newWord) → _context.Add → RedirectToAction(Index)
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/vocabulary")]
        public async Task<IActionResult> AddWord([FromBody] CreateVocabDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.ErrorResponse("Dữ liệu không hợp lệ", errors));
            }

            // Validate topic tồn tại
            if (!await _context.VocabularyTopics.AnyAsync(t => t.TopicId == dto.TopicId))
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn chủ đề hợp lệ."));

            var word = new Vocabulary
            {
                Word      = dto.Word,
                Meaning   = dto.Meaning,
                Example   = dto.Example,
                Note      = dto.Note,
                TopicId   = dto.TopicId,
                CreatedAt = DateTime.Now
            };

            _context.Vocabulary.Add(word);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTopicVocabulary), new { id = dto.TopicId },
                ApiResponse<VocabWordDto>.SuccessResponse(new VocabWordDto
                {
                    VocabId   = word.VocabId,
                    Word      = word.Word,
                    Meaning   = word.Meaning,
                    Example   = word.Example,
                    Note      = word.Note,
                    TopicId   = word.TopicId,
                    CreatedAt = word.CreatedAt
                }, "Thêm từ vựng thành công!"));
        }

        // ────────────────────────────────────────────────────────────
        // DELETE /api/admin/vocabulary/{id}
        // MVC cũ: DeleteWord(id) → _context.Remove → RedirectToAction(Index)
        // ────────────────────────────────────────────────────────────
        [HttpDelete("api/admin/vocabulary/{id}")]
        public async Task<IActionResult> DeleteWord(int id)
        {
            var vocab = await _context.Vocabulary.FindAsync(id);
            if (vocab == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy từ vựng."));

            try
            {
                _context.Vocabulary.Remove(vocab);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse("Lỗi khi xóa: " + ex.Message));
            }
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/vocabulary/topics
        // MVC cũ: ViewTopics() — danh sách topic để xem từ vựng
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/vocabulary/topics")]
        public async Task<IActionResult> GetTopics()
        {
            var topics = await _context.VocabularyTopics
                .Include(t => t.Vocabularies)
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    t.TopicId,
                    t.Name,
                    WordCount = t.Vocabularies != null ? t.Vocabularies.Count : 0
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(topics));
        }

        // ────────────────────────────────────────────────────────────
        // GET /api/admin/vocabulary/topics/{id}
        // MVC cũ: Details(id) — xem từ vựng của 1 topic
        // ────────────────────────────────────────────────────────────
        [HttpGet("api/admin/vocabulary/topics/{id}")]
        public async Task<IActionResult> GetTopicVocabulary(int id)
        {
            var topic = await _context.VocabularyTopics.FindAsync(id);
            if (topic == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Không tìm thấy chủ đề."));

            var words = await _context.Vocabulary
                .Where(w => w.TopicId == id)
                .Select(w => new VocabWordDto
                {
                    VocabId   = w.VocabId,
                    Word      = w.Word,
                    Meaning   = w.Meaning,
                    Example   = w.Example,
                    Note      = w.Note,
                    TopicId   = w.TopicId,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                TopicId   = topic.TopicId,
                TopicName = topic.Name,
                Words     = words
            }));
        }

        // ────────────────────────────────────────────────────────────
        // POST /api/admin/vocabulary/import
        // Form: excelFile (IFormFile) + importTopicId (int)
        // MVC cũ: ImportVocabularyFromExcel(excelFile, importTopicId) — đọc ClosedXML
        // ────────────────────────────────────────────────────────────
        [HttpPost("api/admin/vocabulary/import")]
        public async Task<IActionResult> ImportFromExcel(IFormFile excelFile, [FromForm] int importTopicId)
        {
            if (importTopicId == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn Chủ đề để thêm từ vựng vào."));

            if (excelFile == null || excelFile.Length == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse("Vui lòng chọn file Excel."));

            if (!await _context.VocabularyTopics.AnyAsync(t => t.TopicId == importTopicId))
                return BadRequest(ApiResponse<object>.ErrorResponse("Chủ đề không tồn tại."));

            int successCount = 0;
            int currentRow   = 2;

            try
            {
                using var stream = new MemoryStream();
                await excelFile.CopyToAsync(stream);

                using var workbook  = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);
                var lastRow   = worksheet.LastRowUsed()?.RowNumber() ?? 1;

                for (int row = 2; row <= lastRow; row++)
                {
                    currentRow = row;
                    var excelRow = worksheet.Row(row);

                    var wordText    = excelRow.Cell(1).GetValue<string>()?.Trim();
                    var meaningText = excelRow.Cell(2).GetValue<string>()?.Trim();
                    var exampleText = excelRow.Cell(3).GetValue<string>()?.Trim();

                    if (string.IsNullOrEmpty(wordText)) continue;

                    _context.Vocabulary.Add(new Vocabulary
                    {
                        Word      = wordText,
                        Meaning   = meaningText,
                        Example   = exampleText,
                        TopicId   = importTopicId,
                        CreatedAt = DateTime.Now
                    });
                    successCount++;
                }

                if (successCount > 0)
                    await _context.SaveChangesAsync();
                else
                    return BadRequest(ApiResponse<object>.ErrorResponse("File không có dữ liệu hợp lệ."));

                return Ok(ApiResponse<object>.SuccessResponse($"Đã thêm thành công {successCount} từ vựng!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Lỗi tại dòng {currentRow}: {ex.Message}"));
            }
        }
    }
}

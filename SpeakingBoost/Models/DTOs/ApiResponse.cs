namespace SpeakingBoost.Models.DTOs
{
    /// <summary>
    /// Chuẩn hoá JSON response cho toàn bộ API — thay thế TempData/ViewBag của MVC
    /// Ví dụ thành công: { "success": true, "message": "...", "data": {...} }
    /// Ví dụ lỗi:        { "success": false, "message": "...", "errors": [...] }
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        // ---- Factory methods ----

        public static ApiResponse<T> SuccessResponse(T data, string message = "Thành công")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> SuccessResponse(string message = "Thành công")
            => new() { Success = true, Message = message };

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors };
    }
}

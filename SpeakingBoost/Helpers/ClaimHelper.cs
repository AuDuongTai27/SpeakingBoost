using System.Security.Claims;

namespace SpeakingBoost.Helpers
{
    public static class ClaimHelper
    {
        /// <summary>
        /// Lấy StudentId từ JWT claim "StudentId".
        /// Trả về null nếu claim không tồn tại hoặc không parse được.
        /// </summary>
        public static int? GetStudentId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst("StudentId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        /// <summary>
        /// Lấy Redirect URL dựa trên role của người dùng.
        /// </summary>
        public static string GetRedirectUrl(string? role)
        {
            return role?.Trim().ToLower() switch
            {
                "user"  => "/student/homepage.html",
                "admin" => "/admin/dashboard.html",
                _       => "/login.html"
            };
        }
    }
}

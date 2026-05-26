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
    }
}

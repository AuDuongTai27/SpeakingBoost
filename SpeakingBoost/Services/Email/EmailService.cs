using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SpeakingBoost.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var mailSettings = _configuration.GetSection("MailSettings");
            var fromEmail = mailSettings["Mail"];
            var displayName = mailSettings["DisplayName"];
            var password = mailSettings["Password"];
            var host = mailSettings["Host"];
            var port = int.Parse(mailSettings["Port"]);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(displayName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml) bodyBuilder.HtmlBody = body;
            else bodyBuilder.TextBody = body;
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(fromEmail, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi gửi email: {ex.Message}");
            }
        }

        public Task SendNewOrOverduePostNotification(string studentEmail, string postTitle, string status)
        {
            string subject = $"📢 Thông báo về bài tập: {postTitle}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>Thông báo bài tập</h2>
                    <p>Xin chào,</p>
                    <p>Bài tập <strong>{postTitle}</strong> đã <strong>{status}</strong>.</p>
                    <p>Vui lòng truy cập hệ thống để kiểm tra chi tiết.</p>
                    <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='color: #6b7280; font-size: 12px;'>Email tự động từ Diamond IELTS</p>
                </div>";
            return SendEmailAsync(studentEmail, subject, body);
        }

        public Task SendLowScoreWarning(string studentEmail, int failedAttempts)
        {
            string subject = "⚠️ Cảnh báo điểm thấp - Cần cải thiện";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #dc2626;'>Cảnh báo điểm thấp</h2>
                    <p>Xin chào,</p>
                    <p>Hệ thống nhận thấy bạn đã đạt điểm thấp <strong>{failedAttempts} lần</strong> liên tiếp.</p>
                    <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='color: #6b7280; font-size: 12px;'>Email tự động từ Diamond IELTS</p>
                </div>";
            return SendEmailAsync(studentEmail, subject, body);
        }

        public Task SendDeadlineNotification(string studentEmail, string exerciseTitle, string className, DateTime deadline)
        {
            string subject = $"📅 Thông báo Deadline: {exerciseTitle}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>Thông báo Deadline mới</h2>
                    <p>Xin chào,</p>
                    <p>Bài tập <strong>{exerciseTitle}</strong> đã được giao cho lớp <strong>{className}</strong>.</p>
                    <div style='background-color: #fef3c7; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <p style='margin: 0; font-size: 16px;'><strong>⏰ Deadline:</strong> {deadline:dd/MM/yyyy HH:mm}</p>
                    </div>
                    <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='color: #6b7280; font-size: 12px;'>Email tự động từ Diamond IELTS</p>
                </div>";
            return SendEmailAsync(studentEmail, subject, body);
        }

        public Task SendDeadlineReminderNotification(string studentEmail, string exerciseTitle, DateTime deadline, int hoursRemaining)
        {
            string subject = $"⏰ Nhắc nhở: {exerciseTitle} sắp hết hạn!";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #dc2626;'>Nhắc nhở Deadline</h2>
                    <p>Xin chào,</p>
                    <p>Bài tập <strong>{exerciseTitle}</strong> của bạn sắp hết hạn!</p>
                    <div style='background-color: #fee2e2; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                        <p style='margin: 0; font-size: 16px;'><strong>⏰ Deadline:</strong> {deadline:dd/MM/yyyy HH:mm}</p>
                        <p style='margin: 5px 0 0 0; color: #dc2626;'>Còn <strong>{hoursRemaining} giờ</strong> nữa!</p>
                    </div>
                    <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='color: #6b7280; font-size: 12px;'>Email tự động từ Diamond IELTS</p>
                </div>";
            return SendEmailAsync(studentEmail, subject, body);
        }

        public Task SendRecoveredPassword(string studentEmail, string password)
        {
            string subject = "🔑 Khôi phục mật khẩu - Diamond IELTS";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2563eb;'>Lấy lại mật khẩu</h2>
                    <p>Xin chào,</p>
                    <p>Bạn đã yêu cầu lấy lại mật khẩu cho tài khoản liên kết với email này.</p>
                    <div style='background-color: #f3f4f6; padding: 15px; border-radius: 5px; margin: 15px 0; text-align: center;'>
                        <p style='margin: 0; font-size: 18px;'>Mật khẩu của bạn là: <strong>{password}</strong></p>
                    </div>
                    <p>Vui lòng đăng nhập lại và đổi mật khẩu để bảo mật thông tin.</p>
                    <hr style='border: 1px solid #e5e7eb; margin: 20px 0;'>
                    <p style='color: #6b7280; font-size: 12px;'>Email tự động từ Diamond IELTS</p>
                </div>";
            return SendEmailAsync(studentEmail, subject, body);
        }
    }
}
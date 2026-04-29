namespace SpeakingBoost.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendNewOrOverduePostNotification(string studentEmail, string postTitle, string status);
        Task SendLowScoreWarning(string studentEmail, int failedAttempts);
        Task SendDeadlineNotification(string studentEmail, string exerciseTitle, string className, DateTime deadline);
        Task SendDeadlineReminderNotification(string studentEmail, string exerciseTitle, DateTime deadline, int hoursRemaining);
        Task SendRecoveredPassword(string studentEmail, string password);
    }
}
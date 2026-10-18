using System.Threading.Tasks;

namespace ToDoList.Application.Common.Interfaces
{
    public interface IEmailSender
    {
        Task SendOtpEmailAsync(string toEmail, string userName, string otp);
    }

    public interface IEmailConfiguration
    {
        string Host { get; }
        int Port { get; }
        string SmtpUsername { get; }
        string SmtpPassword { get; }
        string SenderEmail { get; }
        string SenderDisplayName { get; }
        bool EnableSsl { get; }
        int ExpirationMinutes { get; }
    }
}

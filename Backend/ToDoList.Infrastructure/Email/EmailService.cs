using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToDoList.Application.Common.Interfaces;

namespace ToDoList.Infrastructure.Email
{
    public class EmailOptions : IEmailConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 2525;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderDisplayName { get; set; } = "ToDo & Notes App";
        public bool EnableSsl { get; set; } = true;
        public int ExpirationMinutes { get; set; } = 15;
    }

    public class EmailService : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string userName, string otp)
        {
            if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.SmtpUsername))
            {
                _logger.LogWarning("Email sending skipped: SMTP not configured. OTP for {Email} is: {Otp}", toEmail, otp);
                return;
            }

            var subject = "كود التحقق الخاص بك - ToDo & Notes";
            var body = $@"
<!DOCTYPE html>
<html dir=""rtl"">
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0c0d0e; color: #f5f5f5; padding: 20px; }}
        .card {{ max-width: 480px; margin: 0 auto; background: #16181a; border: 1px solid #2e3238; border-radius: 12px; padding: 32px; text-align: center; }}
        .otp-code {{ font-size: 36px; letter-spacing: 8px; font-weight: bold; color: #58a6ff; margin: 24px 0; background: #0d1117; padding: 16px; border-radius: 8px; }}
        .footer {{ font-size: 12px; color: #8b949e; margin-top: 24px; }}
    </style>
</head>
<body>
    <div class=""card"">
        <h2>مرحباً {WebUtility.HtmlEncode(userName)}</h2>
        <p>استخدم كود التحقق التالي لتسجيل الدخول إلى حسابك:</p>
        <div class=""otp-code"">{otp}</div>
        <p>صلاحية الكود {_options.ExpirationMinutes} دقيقة. برجاء عدم مشاركته مع أي شخص.</p>
        <div class=""footer"">إذا لم تطلب هذا الرمز، يمكنك تجاهل هذه الرسالة بأمان.</div>
    </div>
</body>
</html>";

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_options.SenderEmail, _options.SenderDisplayName, Encoding.UTF8),
                Subject = subject,
                SubjectEncoding = Encoding.UTF8,
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
                throw;
            }
        }
    }
}

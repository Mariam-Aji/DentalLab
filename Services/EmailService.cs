using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentalLab.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                // قراءة البيانات بالأسماء المطابقة لـ appsettings.json
                var smtpHost = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
                var smtpPort = _config.GetValue<int>("SmtpSettings:Port", 587);
                var username = _config["SmtpSettings:Username"] ?? "";
                var password = _config["SmtpSettings:Password"] ?? "";
                var fromAddress = _config["SmtpSettings:From"] ?? username;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogError("إعدادات البريد (Username/Password) مفقودة في SmtpSettings ضمن appsettings.json");
                    return;
                }

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromAddress, "DentalLab Platform"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"تم إرسال البريد الإلكتروني بنجاح إلى: {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"فشل إرسال البريد الإلكتروني إلى {toEmail}. التفاصيل: {ex.Message}");
            }
        }
    }
}
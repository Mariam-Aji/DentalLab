using System.Net;
using System.Net.Mail;
using DentalLab.Api.Settings;
using Microsoft.Extensions.Options;

namespace DentalLab.Api.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        using var message = new MailMessage(_settings.From, toEmail, subject, body);
        await client.SendMailAsync(message);
    }
}

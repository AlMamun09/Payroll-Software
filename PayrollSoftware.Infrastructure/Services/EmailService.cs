using Microsoft.Extensions.Configuration;
using PayrollSoftware.Infrastructure.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PayrollSoftware.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var settings = _configuration.GetSection("EmailSettings");

            // Get values with defaults
            var smtpServer = settings["SmtpServer"] ?? throw new ArgumentNullException("SmtpServer is required");
            var port = int.Parse(settings["SmtpPort"] ?? "587");
            var username = settings["SmtpUsername"] ?? throw new ArgumentNullException("SmtpUsername is required");
            var password = settings["SmtpPassword"] ?? throw new ArgumentNullException("SmtpPassword is required");
            var fromEmail = settings["FromEmail"] ?? username;
            var enableSsl = bool.Parse(settings["EnableSsl"] ?? "true");

            using var client = new SmtpClient(smtpServer, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            using var mailMessage = new MailMessage(fromEmail, email, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}
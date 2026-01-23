using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace RolixSAEProject.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _cfg;

        public SmtpEmailSender(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public void Send(string to, string subject, string body)
        {
            var host = _cfg["Smtp:Host"];
            var portStr = _cfg["Smtp:Port"];
            var user = _cfg["Smtp:User"];
            var pass = _cfg["Smtp:Pass"];
            var from = _cfg["Smtp:From"];
            var sslStr = _cfg["Smtp:EnableSsl"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
                throw new SmtpException("SMTP non configuré (Smtp:Host / Smtp:From).");

            int port = int.TryParse(portStr, out var p) ? p : 587;
            bool enableSsl = !string.IsNullOrWhiteSpace(sslStr) && sslStr.ToLowerInvariant() == "true";

            using var msg = new MailMessage(from, to, subject, body);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(user))
                client.Credentials = new NetworkCredential(user, pass);

            client.Send(msg);
        }
    }
}

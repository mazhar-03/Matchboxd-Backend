using System.Net;
using System.Net.Mail;

public class EmailService
{
    private readonly string _smtpHost;
    private readonly string _smtpPass;
    private readonly int _smtpPort;
    private readonly string _smtpUser;

    public EmailService(IConfiguration config)
    {
        var smtp = config.GetSection("SmtpSettings");
        _smtpHost = smtp["Host"];
        _smtpPort = int.Parse(smtp["Port"]);
        _smtpUser = smtp["User"];
        _smtpPass = smtp["Pass"];
    }

    public async Task SendVerificationEmailAsync(string toEmail, string username, string verificationLink)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(_smtpUser),
            Subject = "Verify your email",
            Body =
                $"Hi {username},\n\nPlease verify your email by clicking the link below:\n{verificationLink}\n\nThanks!",
            IsBodyHtml = false
        };
        mail.To.Add(toEmail);

        using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUser, _smtpPass),
            EnableSsl = true
        };

        await smtpClient.SendMailAsync(mail);
    }
}
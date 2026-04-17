using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Notifications.API.Application.Interfaces;
using Notifications.API.Application.Settings;

namespace Notifications.API.Application.Services;

public class EmailService(IOptions<EmailSettings> emailSettings) : IEmailService
{
    private readonly EmailSettings _emailSettings = emailSettings.Value;

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        try
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(_emailSettings.DisplayName, _emailSettings.Mail));

            email.To.Add(MailboxAddress.Parse(toEmail));

            email.Subject = subject;

            var builder = new BodyBuilder();
            if (isHtml)
                builder.HtmlBody = body;
            else
                builder.TextBody = body;

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.Mail, _emailSettings.Password);

            await smtp.SendAsync(email);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[E-POSTA HATASI]: E-posta gönderilemedi. Sebep: {ex.Message}");
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string name)
    {
        string subject = "🚀 Welcome to CryptoMarket!";

        string body = $@"
    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; background-color: #ffffff;'>
        
        <div style='text-align: center; padding-bottom: 20px; border-bottom: 2px solid #0052cc;'>
            <h2 style='color: #0052cc; margin: 0; letter-spacing: 1px;'>CryptoMarket</h2>
        </div>
        
        <div style='padding: 20px 0; color: #333333; line-height: 1.6;'>
            <h3 style='color: #2c3e50;'>Hello {name},</h3>
            <p>Your registration has been completed successfully. We are thrilled to have you step into the crypto world!</p>
            <p>You can now track real-time markets, set smart price alerts, and trade securely.</p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <a href='https://yourstockmarket.com/login' style='background-color: #0052cc; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px;'>Log In to Platform</a>
            </div>
        </div>
        
        <div style='margin-top: 20px; padding-top: 20px; border-top: 1px solid #e0e0e0; font-size: 12px; color: #7f8c8d; line-height: 1.5;'>
            <p><strong>⚠️ Security Warning:</strong> CryptoMarket staff will never ask for your password, 2FA code, or wallet seed phrase. Please do not share this information with anyone.</p>
            <p>If you did not create this account, please contact our <a href='mailto:support@cryptomarket.com' style='color: #0052cc;'>support team</a> immediately.</p>
            <p style='text-align: center; margin-top: 20px;'>&copy; {DateTime.Now.Year} CryptoMarket. All rights reserved.</p>
        </div>

    </div>";

        await SendEmailAsync(email, subject, body, true);
    }
}

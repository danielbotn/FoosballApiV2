using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Models.Accounts;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Newtonsoft.Json;

namespace FoosballApi
{
    public interface IEmailService
    {
        void Send(string to, string subject, string html, string from = null);
        Task SendVerificationEmail(VerificationModel vModel, User user, string origin);
        Task SendPasswordResetEmail(VerificationModel vModel, User user, string origin, ForgotPasswordRequest request);
    }

    public class EmailData
    {
        public string from { get; set; }
        public string[] to { get; set; }
        public string subject { get; set; }
        public string html { get; set; }
        public EmailHeaders headers { get; set; }
        public EmailAttachment[] attachments { get; set; }
    }

    public class EmailHeaders
    {
        public string XEntityRefID { get; set; }
    }

    public class EmailAttachment
    {
        public string filename { get; set; }
        public byte[] content { get; set; }
    }

    public class EmailService : IEmailService
    {
        public EmailService()
        {
        
        }

       public async Task<string> SendWithApi(string to, string subject, string html, string from = null)
        {
            HttpCaller httpCaller = new();

            // Construct the request body for sending an email using Resend API
            EmailData emailData = new()
            {
                from = "Acme <onboarding@resend.dev>",
                to = new[] { to },
                subject = subject,
                html = html,
                headers = new EmailHeaders
                {
                    XEntityRefID = "123"
                }
            };

            string bodyParam = JsonConvert.SerializeObject(emailData);
            string URL = "https://api.resend.com/emails";

            // Make the API call to send the email using Resend API
            string data = await httpCaller.CallSenderApi(bodyParam, URL);
            return data;
        }


        public void Send(string to, string subject, string html, string from = null)
        {
            var smtpEmailFrom = Environment.GetEnvironmentVariable("SmtpEmailFrom");
            var smtpHost = Environment.GetEnvironmentVariable("resendSmtpHost");
            var smtpPort = Environment.GetEnvironmentVariable("resendSmtpPort");
            var smtpUser = Environment.GetEnvironmentVariable("resendSmtpUser");
            var smtpPass = Environment.GetEnvironmentVariable("resendSmtpPassword");
            // create message
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("onboarding@resend.dev"));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = html };

            // send email
            using var smtp = new SmtpClient();
            smtp.Connect(smtpHost, int.Parse(smtpPort), SecureSocketOptions.StartTls);
            smtp.Authenticate(smtpUser, smtpPass);
            smtp.Send(email);
            smtp.Disconnect(true);
        }

        public async Task SendPasswordResetEmail(VerificationModel vModel, User user, string origin, ForgotPasswordRequest request)
        {
            string message;

            message = $"<h1>RESET YOUR PASSWORD</h1>";

            message +=  $"<p>Hi {user.FirstName}, </p>";

            message += "<p>Lost your password? Click the button below and follow the instructions to change your DANO password.</p>";
            message += "<p>If you didn't change your password, or if you have any questions, please contact Customer Support.</p>";

            string combinedInfo = $"{user.Id}-{user.FirstName}-{vModel.PasswordResetToken}-{vModel.PasswordResetTokenExpires}";
            string encryptCobinedInfo = EncryptionHelper.EncryptString(combinedInfo);
            string resetUrl = $"http://localhost:5173/forgotPassword/{encryptCobinedInfo}";

            message += $"<a href='{resetUrl}' style='background-color: green; color: white; padding: 15px 25px; text-align: center; text-decoration: none; display: inline-block; border-radius: 5px;'>Reset Password</a>";

            await SendWithApi(
                to: user.Email,
                subject: "Dano Foosball - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                        {message}",
                from: request.Email
            );
        }

        public async Task SendVerificationEmail(VerificationModel vModel, User user, string origin)
        {
            string verificationCode = $"<div style=\"background-color: #008000; padding: 10px; border-radius: 5px;\"><p style=\"font-size: 18px; color: #ffffff; text-align: center;\">{vModel.VerificationToken}</p></div>";

            string message = $@"<p>Hi {user.FirstName},</p>
                                <p>Here is your verification code to verify your Dano email:</p>
                                {verificationCode}
                                <p>Regards,</p>
                                <p>Dano Support</p>";

            await SendWithApi(
                to: "danielfrs87@gmail.com",
                subject: "Dano Account Activation",
                html: message
            );
        }

    }
}
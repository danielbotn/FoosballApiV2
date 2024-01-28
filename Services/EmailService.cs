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
        void SendVerificationEmail(VerificationModel vModel, User user, string origin);
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
            if (!string.IsNullOrEmpty(origin))
            {
                var resetUrl = $"{origin}/auth/reset-password?token={vModel.PasswordResetToken}";
                message = $@"<p>Please click the below link to reset your password, the link will be valid for 1 day:</p>
                             <p><a href=""{resetUrl}"">{resetUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to reset your password with the <code>/accounts/reset-password</code> api route:</p>
                             <p><code>{vModel.PasswordResetToken}</code></p>";
            }

            await SendWithApi(
                to: user.Email,
                subject: "Dano Foosball - Reset Password",
                html: $@"<h4>Reset Password Email</h4>
                         {message}",
                from: request.Email
            );
        }

        public void SendVerificationEmail(VerificationModel vModel, User user, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                message = $@"<p>Your conformation code:</p>
                             <p>{vModel.VerificationToken}</p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{vModel.VerificationToken}</code></p>";
            }

            Send(
                to: user.Email,
                subject: "Foosball - Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }
    }
}
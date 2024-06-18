namespace FoosballApi.Models.Accounts
{
    public class UpdatePasswordModel
    {
        public bool VerificationCodeCreated { get; set; } = false;
        public bool VerificationCodeEmailSent { get; set; } = false;
        public bool PasswordUpdated { get; set; } = false;
        public VerificationModel VerificationModel { get; set; }
    }
}
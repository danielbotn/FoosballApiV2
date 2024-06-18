namespace FoosballApi.Dtos.Users
{
    public class UpdatePasswordResponse
    {
        public bool EmailSent { get; set; }
        public bool VerificationCodeCreated { get; set; }
        public bool PasswordUpdated { get; set; }
    }
}
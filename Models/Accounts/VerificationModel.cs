using System.ComponentModel.DataAnnotations;

namespace FoosballApi
{
    public class VerificationModel
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string VerificationToken { get; set; }

        public string PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpires { get; set; }

        [Required]
        public bool HasVerified { get; set; }

        public string ChangePasswordToken { get; set; } = null;
        public DateTime? ChangePasswordTokenExpires { get; set; } = null;
        public string ChangePasswordVerificationToken { get; set; } = null;

    }
}

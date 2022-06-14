using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FoosballApi.Models;

namespace FoosballApi
{
    public class VerificationModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public string VerificationToken { get; set; }

        public string PasswordResetToken { get; set; }

        public DateTime? PasswordResetTokenExpires { get; set; }

        [Required]
        public bool HasVerified { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}

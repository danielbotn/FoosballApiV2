using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Accounts
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Token { get; set; }
        [Required]
        public int UserId { get; set; }
    }
}
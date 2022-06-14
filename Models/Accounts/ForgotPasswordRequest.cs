using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Accounts
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
    }
}
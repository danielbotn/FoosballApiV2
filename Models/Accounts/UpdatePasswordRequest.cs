using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Accounts
{
    public class UpdatePasswordRequest
    {
        [Required]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
        
        public string VerficationCode { get; set; } = null;
    }
}
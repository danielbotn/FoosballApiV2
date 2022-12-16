using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models
{
    public class TokenApiModel
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}

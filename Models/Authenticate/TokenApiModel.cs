using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models
{
    public class TokenApiModel
    {
        [Required]
        public string AccessToken { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}

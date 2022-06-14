using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.Users
{
    public class UserUpdateDto
    {

        [Required]
        [MaxLength(250)]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public int? CurrentOrganisationId { get; set; }

    }
}
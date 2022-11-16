using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Dtos.Users
{
    public class UserReadDto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Email { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public int? CurrentOrganisationId { get; set; }

        public string PhotoUrl { get; set; }
        public bool? IsAdmin { get; set; }
        public bool IsDeleted { get; set; }
    }
}
namespace FoosballApi.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public DateTime Created_at { get; set; }

        public int? CurrentOrganisationId { get; set; }

        public string PhotoUrl { get; set; }
        public bool? IsAdmin { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public string AuthProvider { get; set; }
        public string GoogleId { get; set; }

    }
}

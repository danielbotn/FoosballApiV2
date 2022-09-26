namespace FoosballApi.Dtos.Users
{
    public class UserReadJoinDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CurrentOrganisationId { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsAdmin { get; set; }
    }
}
namespace FoosballApi.Models.OldRefreshTokens
{
    public class OldRefreshToken
    {
        public int Id { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }
        public int UserId { get; set; }
        public int OrganisationId { get; set; }
    }
}
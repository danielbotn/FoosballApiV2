using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.Organisations
{
    public class OrganisationReadDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public OrganisationType OrganisationType { get; set; }

        [Required]
        public string OrganisationCode { get; set; }
        public string SlackWebhookUrl { get; set; }
        public string DiscordWebhookUrl { get; set; }
        public string MicrosoftTeamsWebhookUrl { get; set; }
    }
}
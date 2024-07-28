using System.ComponentModel.DataAnnotations;
using FoosballApi.Dtos.Organisations;

namespace FoosballApi
{
    public class OrganisationModel
    {
        [Required]
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
        public string MicrosoftTeamWebhookUrl { get; set; }
    }
}
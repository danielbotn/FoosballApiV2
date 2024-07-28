using System;
using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Dtos.Organisations
{
    public class OrganisationUpdateDto
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public OrganisationType OrganisationType { get; set; }
        public string SlackWebhookUrl { get; set; }
        public string DiscordWebhookUrl { get; set; }
        public string MicrosoftTeamsWebhookUrl { get; set; }
    }
}
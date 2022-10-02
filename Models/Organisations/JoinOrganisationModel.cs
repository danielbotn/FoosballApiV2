using System.ComponentModel.DataAnnotations;

namespace FoosballApi.Models.Organisations
{
    public class JoinOrganisationModel
    {
        [Required]
        public string OrganisationCodeAndOrganisationId { get; set; }
    }
}
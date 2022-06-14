using System;
using System.ComponentModel.DataAnnotations;
using FoosballApi.Dtos.Organisations;

namespace FoosballApi.Models.Organisations
{
    public class OrganisationModelCreate
    {

        [Required]
        public string Name { get; set; }

        [Required]
        public OrganisationType OrganisationType { get; set; }

    }
}
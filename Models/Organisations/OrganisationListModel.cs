using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models
{
    public class OrganisationListModel
    {
        public int Id { get; set; }
        public int OrganisationId { get; set; }
        public int UserId { get; set; }
    }
}
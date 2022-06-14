using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Matches
{
    public class FreehandMatchModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int PlayerOneId { get; set; }

        [ForeignKey("PlayerOneId")]
        public virtual User User { get; set; }

        [Required]
        public int PlayerTwoId { get; set; }

        [ForeignKey("PlayerTwoId")]
        public virtual User user { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        public int PlayerOneScore { get; set; }

        [Required]
        public int PlayerTwoScore { get; set; }

        [Required]
        public int UpTo { get; set; }

        [Required]
        public bool GameFinished { get; set; }

        [Required]
        public bool GamePaused { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [ForeignKey("OrganisationId")]
        public virtual OrganisationModel OrganisationModel { get; set; }

    }
}

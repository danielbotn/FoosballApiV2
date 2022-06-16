using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoosballApi.Models.Matches
{
    public class FreehandMatchModel
    {
        public int Id { get; set; }
        public int PlayerOneId { get; set; }
        public int PlayerTwoId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int PlayerOneScore { get; set; }
        public int PlayerTwoScore { get; set; }
        public int UpTo { get; set; }
        public bool GameFinished { get; set; }
        public bool GamePaused { get; set; }
        public int OrganisationId { get; set; }

    }
}

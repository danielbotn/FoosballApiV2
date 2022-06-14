using System;

namespace FoosballApi.Models.Matches
{
    public class FreehandMatchModelExtended
    {
        public int Id { get; set; }
        public int PlayerOneId { get; set; }
        public string PlayerOneFirstName { get; set; }
        public string PlayerOneLastName { get; set; }
        public string PlayerOnePhotoUrl { get; set; }
        public int PlayerTwoId { get; set; }
        public string PlayerTwoFirstName { get; set; }
        public string PlayerTwoLastName { get; set; }
        public string PlayerTwoPhotoUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string TotalPlayingTime { get; set; }
        public int PlayerOneScore { get; set; }
        public int PlayerTwoScore { get; set; }
        public int UpTo { get; set; }
        public bool GameFinished { get; set; }
        public bool GamePaused { get; set; }
    }
}
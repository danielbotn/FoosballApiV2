using System;

namespace FoosballApi.Dtos.DoubleGoals
{
    public class FreehandDoubleGoalsJoinDto
    {
        public int Id { get; set; }
        public int ScoredByUserId { get; set; }
        public int DoubleMatchId { get; set; }
        public int ScorerTeamScore { get; set; }
        public int OpponentTeamScore { get; set; }
        public bool WinnerGoal { get; set; }
        public DateTime TimeOfGoal { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhotoUrl { get; set; }
    }
}
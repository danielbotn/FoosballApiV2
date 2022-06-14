using System;

namespace FoosballApi.Models.DoubleLeagueGoals
{
    public class DoubleLeagueGoalDapper
    {
        public int Id { get; set; }
        public DateTime TimeOfGoal { get; set; }
        public int ScoredByTeamId { get; set; }
        public int OpponentTeamId { get; set; }
        public int ScorerTeamScore { get; set; }
        public int OpponentTeamScore { get; set; }
        public bool WinnerGoal { get; set; }
        public int UserScorerId { get; set; }
        public int DoubleLeagueTeamId { get; set; }
        public string ScorerFirstName { get; set; }
        public string ScorerLastName { get; set; }
        public string ScorerPhotoUrl { get; set; }
    }
}
using System;

namespace FoosballApi.Dtos.SingleLeagueGoals
{
    public class SingleLeagueGoalReadDto
    {
        public int Id { get; set; }
        public DateTime TimeOfGoal { get; set; }
        public int MatchId { get; set; }
        public int ScoredByUserId { get; set; }
        public string ScoredByUserFirstName { get; set; }
        public string ScoredByUserLastName { get; set; }
        public string ScoredByUserPhotoUrl { get; set; }
        public int OpponentId { get; set; }
        public string OpponentFirstName { get; set; }
        public string OpponentLastName { get; set; }
        public string OpponentPhotoUrl { get; set; }
        public int ScorerScore { get; set; }
        public int OpponentScore { get; set; }
        public bool WinnerGoal { get; set; }
        public string GoalTimeStopWatch { get; set; }
    }
}
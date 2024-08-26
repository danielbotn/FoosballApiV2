using Newtonsoft.Json;

namespace FoosballApi.Models.Matches
{
    public class DoubleFreehandMatchRealTime
    {
        [JsonProperty("match_id")]
        public int MatchId { get; set; }

        [JsonProperty("team_a_player_one_id")]
        public int TeamAPlayerOneId { get; set; }

        [JsonProperty("team_a_player_two_id")]
        public int TeamAPlayerTwoId { get; set; }

        [JsonProperty("team_b_player_one_id")]
        public int TeamBPlayerOneId { get; set; }

        [JsonProperty("team_b_player_two_id")]
        public int TeamBPlayerTwoId { get; set; }

        [JsonProperty("team_a_score")]
        public int TeamAScore { get; set; }

        [JsonProperty("team_b_score")]
        public int TeamBScore { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("up_to")]
        public int UpTo { get; set; }

        [JsonProperty("game_finished")]
        public bool GameFinished { get; set; }

        [JsonProperty("game_paused")]
        public bool GamePaused { get; set; }

        [JsonProperty("organisation_id")]
        public int OrganisationId { get; set; }

        [JsonProperty("team_a_player_one")]
        public PlayerInfo TeamAPlayerOne { get; set; }

        [JsonProperty("team_a_player_two")]
        public PlayerInfo TeamAPlayerTwo { get; set; }

        [JsonProperty("team_b_player_one")]
        public PlayerInfo TeamBPlayerOne { get; set; }

        [JsonProperty("team_b_player_two")]
        public PlayerInfo TeamBPlayerTwo { get; set; }

        [JsonProperty("last_goal")]
        public GoalInfo LastGoal { get; set; }
    }

    public class GoalInfo
    {
        [JsonProperty("scored_by_user_id")]
        public int ScoredByUserId { get; set; }

        [JsonProperty("scorer_team_score")]
        public int ScorerTeamScore { get; set; }

        [JsonProperty("opponent_team_score")]
        public int OpponentTeamScore { get; set; }

        [JsonProperty("time_of_goal")]
        public DateTime TimeOfGoal { get; set; }

        [JsonProperty("winner_goal")]
        public bool WinnerGoal { get; set; }

        [JsonProperty("scorer")]
        public PlayerInfo Scorer { get; set; }
    }
}

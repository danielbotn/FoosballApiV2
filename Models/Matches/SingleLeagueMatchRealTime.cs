using Newtonsoft.Json;

namespace FoosballApi.Models.Matches
{
    public class SingleLeagueMatchRealTime
    {
        [JsonProperty("match_id")]
        public int MatchId { get; set; }
        
        [JsonProperty("league_id")]
        public int LeagueId { get; set; }

        [JsonProperty("player_one_id")]
        public int PlayerOneId { get; set; }

        [JsonProperty("player_two_id")]
        public int PlayerTwoId { get; set; }

        [JsonProperty("player_one_score")]
        public int PlayerOneScore { get; set; }

        [JsonProperty("player_two_score")]
        public int PlayerTwoScore { get; set; }

        [JsonProperty("start_time")]
        public DateTime? StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("up_to")]
        public int UpTo { get; set; }

        [JsonProperty("match_ended")]
        public bool MatchEnded { get; set; }

        [JsonProperty("match_paused")]
        public bool MatchPaused { get; set; }

        [JsonProperty("organisation_id")]
        public int OrganisationId { get; set; }

        [JsonProperty("last_goal")]
        public GoalInfo LastGoal { get; set; }

        [JsonProperty("player_one")]
        public PlayerInfo PlayerOne { get; set; }

        [JsonProperty("player_two")]
        public PlayerInfo PlayerTwo { get; set; }
    }
}

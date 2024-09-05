using Newtonsoft.Json;

namespace FoosballApi.Models.Matches
{
    public class DoubleLeagueMatchRealTime
    {
        [JsonProperty("match_id")]
        public int MatchId { get; set; }

        [JsonProperty("team_one_id")]
        public int TeamOneId { get; set; }

        [JsonProperty("team_two_id")]
        public int TeamTwoId { get; set; }

        [JsonProperty("team_one_score")]
        public int TeamOneScore { get; set; }

        [JsonProperty("team_two_score")]
        public int TeamTwoScore { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

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

        [JsonProperty("team_one_players")]
        public List<PlayerInfo> TeamOnePlayers { get; set; }

        [JsonProperty("team_two_players")]
        public List<PlayerInfo> TeamTwoPlayers { get; set; }
        
        [JsonProperty("league_id")]
        public int LeagueId { get; set; }
    }
}

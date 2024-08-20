using Newtonsoft.Json;
using System;

namespace FoosballApi.Models.Matches
{
    public class FreehandMatchRealTime
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("player_one_id")]
        public int PlayerOneId { get; set; }

        [JsonProperty("player_two_id")]
        public int PlayerTwoId { get; set; }

        [JsonProperty("start_time")]
        public DateTime StartTime { get; set; }

        [JsonProperty("end_time")]
        public DateTime? EndTime { get; set; }

        [JsonProperty("player_one_score")]
        public int PlayerOneScore { get; set; }

        [JsonProperty("player_two_score")]
        public int PlayerTwoScore { get; set; }

        [JsonProperty("up_to")]
        public int UpTo { get; set; }

        [JsonProperty("game_finished")]
        public bool GameFinished { get; set; }

        [JsonProperty("game_paused")]
        public bool GamePaused { get; set; }

        [JsonProperty("organisation_id")]
        public int OrganisationId { get; set; }
    }
}

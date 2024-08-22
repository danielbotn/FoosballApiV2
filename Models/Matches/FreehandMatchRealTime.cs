using Newtonsoft.Json;
using System;

namespace FoosballApi.Models.Matches
{
    public class FreehandMatchRealTime
    {
        [JsonProperty("match_id")]
        public int MatchId { get; set; }

        [JsonProperty("player_one_id")]
        public int PlayerOneId { get; set; }

        [JsonProperty("player_two_id")]
        public int PlayerTwoId { get; set; }

        [JsonProperty("player_one")]
        public PlayerInfo PlayerOne { get; set; }

        [JsonProperty("player_two")]
        public PlayerInfo PlayerTwo { get; set; }

        [JsonProperty("player_one_score")]
        public int PlayerOneScore { get; set; }

        [JsonProperty("player_two_score")]
        public int PlayerTwoScore { get; set; }

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
    }

    public class PlayerInfo
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("photo_url")]
        public string PhotoUrl { get; set; }
    }
}

using System;

namespace FoosballApi.Models.Users
{
    /* 
    select distinct dlm.id, dlm.team_one_id, dlm.team_two_id, dlm.start_time, dlm.end_time, dlm.team_one_score, dlm.team_two_score, dlm.match_ended
from double_league_matches dlm
join double_league_players dlp on dlm.team_one_id = dlp.double_league_team_id or dlm.team_two_id = dlp.double_league_team_id
    */
    public class UserLastTenDapper
    {
        public int Id { get; set; }
        public int TeamOneId { get; set; }
        public int TeamTwoId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TeamOneScore { get; set; }
        public int TeamTwoScore { get; set; }
        public bool MatchEnded { get; set; }
    }
}
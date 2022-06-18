using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FoosballApi.Dtos.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueMatches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeagueGoalService
    {
        Task<IEnumerable<DoubleLeagueGoalExtended>> GetAllDoubleLeagueGoalsByMatchId(int matchId);
        // Task<DoubleLeagueGoalDapper> GetDoubleLeagueGoalById(int goalId);
        // bool CheckPermissionByGoalId(int goalId, int userId);
        // DoubleLeagueGoalModel CreateDoubleLeagueGoal(DoubleLeagueGoalCreateDto doubleLeagueGoalCreateDto);
        // void DeleteDoubleLeagueGoal(int goalId);
    }

    public class DoubleLeagueGoalService : IDoubleLeagueGoalService
    {
        public string _connectionString { get; }
        public DoubleLeagueGoalService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        // public bool CheckPermissionByGoalId(int goalId, int userId)
        // {
        //     bool result = false;
        //     List<int> teamIds = new List<int>();
        //     int matchId = _context.DoubleLeagueGoals.FirstOrDefault(x => x.Id == goalId).MatchId;
        //     var matchData = _context.DoubleLeagueMatches.FirstOrDefault(x => x.Id == matchId);
        //     int leagueId = matchData.LeagueId;

        //     teamIds.Add(matchData.TeamOneId);
        //     teamIds.Add(matchData.TeamTwoId);

        //     foreach (var item in teamIds)
        //     {
        //         var doubleLeaguePlayerData = _context.DoubleLeaguePlayers.Where(x => x.DoubleLeagueTeamId == item);

        //         foreach (var element in doubleLeaguePlayerData)
        //         {
        //             if (element.UserId == userId)
        //             {
        //                 result = true;
        //                 break;
        //             }

        //         }

        //     }

        //     return result;
        // }

        // public DoubleLeagueGoalModel CreateDoubleLeagueGoal(DoubleLeagueGoalCreateDto doubleLeagueGoalCreateDto)
        // {
        //     DateTime now = DateTime.Now;
        //     DoubleLeagueGoalModel newGoal = new();
        //     newGoal.TimeOfGoal = now;
        //     newGoal.MatchId = doubleLeagueGoalCreateDto.MatchId;
        //     newGoal.ScoredByTeamId = doubleLeagueGoalCreateDto.ScoredByTeamId;
        //     newGoal.OpponentTeamId = doubleLeagueGoalCreateDto.OpponentTeamId;
        //     newGoal.ScorerTeamScore = doubleLeagueGoalCreateDto.ScorerTeamScore;
        //     newGoal.OpponentTeamScore = doubleLeagueGoalCreateDto.OpponentTeamScore;

        //     if (doubleLeagueGoalCreateDto.WinnerGoal != null)
        //         newGoal.WinnerGoal = (bool)doubleLeagueGoalCreateDto.WinnerGoal;

        //     newGoal.UserScorerId = doubleLeagueGoalCreateDto.UserScorerId;

        //     _context.DoubleLeagueGoals.Add(newGoal);
        //     _context.SaveChanges();

        //     return newGoal;
        // }


        // public void DeleteDoubleLeagueGoal(int goalId)
        // {
        //     var goalToDelete = _context.DoubleLeagueGoals.FirstOrDefault(x => x.Id == goalId);
        //     int scoredByTeamId = goalToDelete.ScoredByTeamId;

        //     var doubleLeagueMatch = _context.DoubleLeagueMatches.FirstOrDefault(x => x.Id == goalToDelete.MatchId);

        //     if (doubleLeagueMatch.TeamOneId == scoredByTeamId)
        //     {
        //         if (doubleLeagueMatch.TeamOneScore > 0)
        //             doubleLeagueMatch.TeamOneScore -= 1;
        //     }

        //     if (doubleLeagueMatch.TeamTwoId == scoredByTeamId)
        //     {
        //         if (doubleLeagueMatch.TeamTwoScore > 0)
        //             doubleLeagueMatch.TeamTwoScore -= 1;
        //     }

        //     _context.DoubleLeagueGoals.Remove(goalToDelete);
        //     _context.SaveChanges();
        // }

        public async Task<List<DoubleLeagueGoalDapper>> GetDoubleLeagueGoals(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<DoubleLeagueGoalDapper>(
                    @"
                    select distinct dlg.id as Id, dlg.time_of_goal as TimeOfGoal, dlg.scored_by_team_id as ScoredByTeam, 
                    dlg.opponent_team_id as OpponentTeamId, dlg.scorer_team_score as ScorerTeamScore, 
                    dlg.opponent_team_score as OpponentTeamScore, dlg.winner_goal as WinnerGoal,
                    dlg.user_scorer_id as UserScorerId, dlp.double_league_team_id as DoubleLeagueTeamId,
                    u.first_name as ScorerFirstName, u.last_name as ScorerLastName, u.photo_url as ScorerPhotoUrl 
                    from double_league_goals dlg
                    join double_league_players dlp on dlp.double_league_team_id = dlg.scored_by_team_id 
                    join users u on u.id = dlg.user_scorer_id 
                    where dlg.match_id = @matchId
                    order by dlg.id
                    ",
                new { matchId });
                return users.ToList();
            }
        }

        public async Task<IEnumerable<DoubleLeagueGoalExtended>> GetAllDoubleLeagueGoalsByMatchId(int matchId)
        {
            var dapperReadData = await GetDoubleLeagueGoals(matchId);
            
            List<DoubleLeagueGoalExtended> result = new();
            
            foreach (var item in dapperReadData)
            {
                DoubleLeagueGoalExtended dlge = new DoubleLeagueGoalExtended{
                    Id = item.Id,
                    TimeOfGoal = item.TimeOfGoal,
                    ScoredByTeamId = item.ScoredByTeamId,
                    OpponentTeamId = item.OpponentTeamId,
                    ScorerTeamScore = item.ScorerTeamScore,
                    OpponentTeamScore = item.OpponentTeamScore,
                    WinnerGoal = item.WinnerGoal,
                    UserScorerId = item.UserScorerId,
                    ScorerFirstName = item.ScorerFirstName,
                    ScorerLastName = item.ScorerLastName,
                    ScorerPhotoUrl = item.ScorerPhotoUrl,
                    GoalTimeStopWatch = CalculateGoalTimeStopWatch(item.TimeOfGoal, matchId),
                };
                result.Add(dlge);
            }

            return result;
        }

        // public async Task<DoubleLeagueGoalDapper> GetDoubleLeagueGoalById(int goalId)
        // {
        //     CancellationToken ct = new();

        //     var tx = await _context.Database.BeginTransactionAsync();

        //     var dapperReadData = await _context.QueryAsync<DoubleLeagueGoalDapper>(ct, $@"
        //         select distinct dlg.id, dlg.time_of_goal, dlg.scored_by_team_id, dlg.opponent_team_id, dlg.scorer_team_score, 
        //         dlg.opponent_team_score, dlg.winner_goal, dlg.user_scorer_id, dlp.double_league_team_id, u.first_name as scorer_first_name, 
        //         u.last_name as scorer_last_name
        //         from double_league_goals dlg
        //         join double_league_players dlp on dlp.double_league_team_id = dlg.scored_by_team_id
        //         join users u on u.id = dlg.user_scorer_id
        //         where dlg.id = {goalId}
        //         order by dlg.id");

        //     return dapperReadData.FirstOrDefault();
        // }

        private DoubleLeagueMatchModel GetDoubleLeagueMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = conn.QueryFirstOrDefault<DoubleLeagueMatchModel>(
                    @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @id",
                    new { matchId });
                return user;
            }
        }

        private string CalculateGoalTimeStopWatch(DateTime timeOfGoal, int matchId)
        {
            var match = GetDoubleLeagueMatchById(matchId);
            DateTime? matchStarted = match.StartTime;
            if (matchStarted == null)
            {
                matchStarted = DateTime.Now;
            }
            TimeSpan timeSpan = matchStarted.Value - timeOfGoal;
            string result = timeSpan.ToString(@"hh\:mm\:ss");
            string sub = result.Substring(0, 2);
            // remove first two characters if they are "00:"
            if (sub == "00")
            {
                result = result.Substring(3);
            }
            return result;
        }
    }
}
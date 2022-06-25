using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeagueTeams;
using FoosballApi.Models.Other;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeaugeMatchService
    {
        Task<bool> CheckMatchAccess(int matchId, int userId, int currentOrganisationId);
        Task<bool> CheckLeaguePermission(int leagueId, int userId);
        Task<IEnumerable<AllMatchesModel>> GetAllMatchesByOrganisationId(int currentOrganisationId, int leagueId);
        Task<DoubleLeagueMatchModel> GetMatchById(int matchId);
        void UpdateDoubleLeagueMatch(DoubleLeagueMatchModel match);
        Task<DoubleLeagueMatchModel> ResetMatch(DoubleLeagueMatchModel doubleLeagueMatchModel, int matchId);
    }

    public class DoubleLeaugeMatchService : IDoubleLeaugeMatchService
    {
        public string _connectionString { get; }

        public DoubleLeaugeMatchService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task<DoubleLeagueMatchModel> GetMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @matchId",
                    new { matchId });
                return match;
            }
        }

       public async Task<List<DoubleLeagueTeamModel>> GetDoubleLeagueTeamsByTeamId(DoubleLeagueMatchModel query)
       {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<DoubleLeagueTeamModel>(
                    @"SELECT id as Id, name as Name, created_at as CreatedAt, 
                    organisation_id as OrganisationId, league_id as LeagueId
                    FROM double_league_teams WHERE id = @teamOneId OR id = @teamTwoId",
                new { teamOneId = query.TeamOneId, teamTwoId = query.TeamTwoId });
                return users.ToList();
            }
        }

        public async Task<bool> CheckMatchAccess(int matchId, int userId, int currentOrganisationId)
        {
            bool result = false;
            var query = await GetMatchById(matchId);
            var query2 = await GetDoubleLeagueTeamsByTeamId(query);

            foreach (var item in query2)
            {
                if (item.OrganisationId == currentOrganisationId)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private async Task<List<LeaguePermissionJoinModel>> GetDoubleLeaguePermissions(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<LeaguePermissionJoinModel>(
                    @"SELECT dlp.id as Id, dlp.user_id as UserId, dlt.league_id as LeagueId
                    FROM double_league_players dlp
                    JOIN double_league_teams dlt ON dlp.double_league_team_id = dlt.id
                    WHERE dlp.user_id = @user_id",
                new { user_id = userId });
                return users.ToList();
            }
        }

        public async Task<bool> CheckLeaguePermission(int leagueId, int userId)
        {
            bool result = false;
            var query = await GetDoubleLeaguePermissions(userId);

            foreach (var item in query)
            {
                if (item.LeagueId == leagueId && item.UserId == userId)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        private async Task<List<AllMatchesModel>> GetDoubleLeagueMatchesByLeagueId(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<AllMatchesModel>(
                    @"SELECT dlm.id as Id, dlm.team_one_id as TeamOneId, dlm.team_two_id as TeamTwoId, dlm.league_id as LeagueId, dlm.start_time as StartTime,
                    dlm.end_time as EndTime, dlm.team_one_score as TeamOneScore, dlm.team_two_score as TeamTwoScore, dlm.match_started as MatchStarted,
                    dlm.match_ended as MatchEnded, dlm.match_paused as MatchPaused
                    FROM double_league_matches dlm
                    WHERE dlm.league_id = @leagueId",
                    new { leagueId });
                return matches.ToList();
            }
        }

        private List<TeamModel> GetSubQuery(AllMatchesModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teams = conn.Query<TeamModel>(
                    @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    WHERE dlp.double_league_team_id = @team_one_id",
                new { team_one_id = item.TeamOneId });
                return teams.ToList();
            }
        }

        private List<TeamModel> GetSubQueryTwo(AllMatchesModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teams = conn.Query<TeamModel>(
                    @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    WHERE dlp.double_league_team_id = @team_two_id",
                new { team_two_id = item.TeamTwoId });
                return teams.ToList();
            }
        }

        public async Task<IEnumerable<AllMatchesModel>> GetAllMatchesByOrganisationId(int currentOrganisationId, int leagueId)
        {
            var query = await GetDoubleLeagueMatchesByLeagueId(leagueId);

            List<AllMatchesModel> result = new List<AllMatchesModel>();

            foreach (var item in query)
            {
                var subquery = GetSubQuery(item);

                var teamOne = subquery.ToArray();

                var subquery2 = GetSubQueryTwo(item);

                var teamTwo = subquery2.ToArray();

                var allTeams = new AllMatchesModel
                {
                    Id = item.Id,
                    TeamOneId = item.TeamOneId,
                    TeamTwoId = item.TeamTwoId,
                    LeagueId = item.LeagueId,
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    TeamOneScore = (int)item.TeamOneScore,
                    TeamTwoScore = (int)item.TeamTwoScore,
                    MatchStarted = (bool)item.MatchStarted,
                    MatchEnded = (bool)item.MatchEnded,
                    MatchPaused = (bool)item.MatchPaused,
                    TeamOne = teamOne,
                    TeamTwo = teamTwo
                };
                result.Add(allTeams);

            }
            return result;
        }

        public void UpdateDoubleLeagueMatch(DoubleLeagueMatchModel match)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE double_league_matches 
                    SET team_one_id = @team_one_Id, 
                    team_two_id = @team_two_id, start_time = @start_time, 
                    end_time = @end_time, team_one_score = @team_one_score, 
                    team_two_score = @team_two_score, match_started = @match_started,
                    match_ended = @match_ended, match_paused = @match_paused
                    WHERE id = @id",
                    new { 
                        team_one_id = match.TeamOneId,
                        team_two_id = match.TeamTwoId,
                        leauge_id = match.LeagueId,
                        start_time = match.StartTime,
                        end_time = match.EndTime,
                        team_one_score = match.TeamOneScore,
                        team_two_score = match.TeamTwoScore,
                        match_started = match.MatchStarted,
                        match_ended = match.MatchEnded,
                        match_paused = match.MatchPaused,
                        id = match.Id
                     });
            }
        }

        private async Task<List<DoubleLeagueGoalModel>> GetDoubleLeagueGoalsByMatchId(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<DoubleLeagueGoalModel>(
                    @"SELECT dlg.id as Id, time_of_goal as TimeOfGoal, dlg.match_id as MatchId, scored_by_team_id as ScoredByTeamId,
                    opponent_team_id as OpponentTeamId, scorer_team_score as ScorerTeamScore, opponent_team_score as OpponentTeamScore,
                    winner_goal as WinnerGoal, user_scorer_id as UserScorerId
                    FROM double_league_goals dlg
                    WHERE dlg.match_id = @match_id",
                new { match_id = matchId });
                return goals.ToList();
            }
        }

        private async void RemoveRange(List<DoubleLeagueGoalModel> goals)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                foreach (var item in goals)
                {
                    await conn.ExecuteAsync(
                        @"DELETE FROM double_league_goals
                        WHERE id = @id",
                        new { id = item.Id });
                }
            }
        }

        private async void DeleteAllGoals(int matchId)
        {
            var allGoals = await GetDoubleLeagueGoalsByMatchId(matchId);	
            RemoveRange(allGoals);
        }

        private async Task<DoubleLeagueMatchModel> ResetDoubleLeagueMatch(DoubleLeagueMatchModel doubleLeagueMatchModel)
        {
            doubleLeagueMatchModel.StartTime = null;
            doubleLeagueMatchModel.EndTime = null;
            doubleLeagueMatchModel.TeamOneScore = 0;
            doubleLeagueMatchModel.TeamTwoScore = 0;
            doubleLeagueMatchModel.MatchStarted = false;
            doubleLeagueMatchModel.MatchEnded = false;
            doubleLeagueMatchModel.MatchPaused = false;

            // use dapper
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"UPDATE double_league_matches 
                    SET start_time = @start_time, 
                    end_time = @end_time, team_one_score = @team_one_score, 
                    team_two_score = @team_two_score, match_started = @match_started,
                    match_ended = @match_ended, match_paused = @match_paused
                    WHERE id = @id",
                    new { 
                        start_time = doubleLeagueMatchModel.StartTime,
                        end_time = doubleLeagueMatchModel.EndTime,
                        team_one_score = doubleLeagueMatchModel.TeamOneScore,
                        team_two_score = doubleLeagueMatchModel.TeamTwoScore,
                        match_started = doubleLeagueMatchModel.MatchStarted,
                        match_ended = doubleLeagueMatchModel.MatchEnded,
                        match_paused = doubleLeagueMatchModel.MatchPaused,
                        id = doubleLeagueMatchModel.Id
                     });
            }

            return doubleLeagueMatchModel;
        }

        public async Task<DoubleLeagueMatchModel> ResetMatch(DoubleLeagueMatchModel doubleLeagueMatchModel, int matchId)
        {
            if (doubleLeagueMatchModel == null)
                throw new ArgumentNullException(nameof(doubleLeagueMatchModel));

            DeleteAllGoals(matchId);

            return await ResetDoubleLeagueMatch(doubleLeagueMatchModel);
        }
    }
}
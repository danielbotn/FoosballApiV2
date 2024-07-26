using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeaguePlayers;
using FoosballApi.Models.DoubleLeagueTeams;
using FoosballApi.Models.Leagues;
using FoosballApi.Models.Other;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeaugeMatchService
    {
        Task<bool> CheckMatchAccess(int matchId, int userId, int currentOrganisationId);
        Task<bool> CheckLeaguePermission(int leagueId, int userId);
        Task<bool> CheckLeaguePermissionByOrganisationId(int leagueId, int currentOrganisationId);
        Task<IEnumerable<AllMatchesModel>> GetAllMatchesByOrganisationId(int currentOrganisationId, int leagueId);
        Task<DoubleLeagueMatchModel> GetMatchById(int matchId);
        void UpdateDoubleLeagueMatch(DoubleLeagueMatchModel match);
        Task<DoubleLeagueMatchModel> ResetMatch(DoubleLeagueMatchModel doubleLeagueMatchModel, int matchId);
        Task<IEnumerable<DoubleLeagueStandingsQuery>> GetDoubleLeagueStandings(int leagueId);
        Task<List<DoubleLeagueMatchModel>> CreateDoubleLeagueMatches(int leagueId, int? howManyRounds);
        Task<List<TeamModel>> GetTeamOne(int teamOneId);
        Task<List<TeamModel>> GetTeamTwo(int teamTwoId);
        Task<TeamMember[]> GetTeamMembers(int teamId);
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

        private static string ToReadableAgeString(TimeSpan span)
        {
            return string.Format("{0:hh\\:mm\\:ss}", span);
        }

        public async Task<DoubleLeagueMatchModel> GetMatchById(int matchId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var match = await conn.QueryFirstOrDefaultAsync<DoubleLeagueMatchModel>(
                @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @matchId",
                new { matchId });

            TimeSpan? playingTime = null;
            if (match.EndTime != null && match.StartTime != null)
            {
                playingTime = match.EndTime - match.StartTime;
            }

            match.TotalPlayingTime = playingTime != null ? ToReadableAgeString(playingTime.Value) : null;
            return match;
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

        public async Task<List<TeamModel>> GetTeamOne(int teamOneId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var team = await conn.QueryAsync<TeamModel>(
                @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email, u.photo_url as PhotoUrl, dlt.name as TeamName,
                    u.id as UserId, dlt.id as TeamId
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    JOIN double_league_teams dlt on dlt.id = @team_one_id
                    WHERE dlp.double_league_team_id = @team_one_id",
            new { team_one_id = teamOneId });
            return team.ToList();
        }

        public async Task<List<TeamModel>> GetTeamTwo(int teamTwoId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var team = await conn.QueryAsync<TeamModel>(
                @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email, u.photo_url as PhotoUrl, dlt.name as TeamName,
                    u.id as UserId, dlt.id as TeamId
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    JOIN double_league_teams dlt on dlt.id = @team_two_id
                    WHERE dlp.double_league_team_id = @team_two_id",
            new { team_two_id = teamTwoId });
            return team.ToList();
        }

        private List<TeamModel> GetSubQuery(AllMatchesModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teams = conn.Query<TeamModel>(
                    @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email, u.photo_url as PhotoUrl, dlt.name as TeamName,
                    u.id as UserId, dlt.id as TeamId
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    JOIN double_league_teams dlt on dlt.id = @team_one_id
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
                    @"SELECT dlp.id as Id, u.first_name as FirstName, u.last_name as LastName, u.email as Email, u.photo_url as PhotoUrl, dlt.name as TeamName,
                    u.id as UserId, dlt.id as TeamId
                    FROM double_league_players dlp
                    JOIN users u ON dlp.user_id = u.id
                    JOIN double_league_teams dlt on dlt.id = @team_two_id
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
            if (match.StartTime != null)
            {
                match.StartTime = DateTime.Now;
            }
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

        private async Task<List<int>> GetAllTeamIds(int leagueId)
        {
            List<int> result = new();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<int>(
                    @"SELECT DISTINCT id
                    FROM double_league_teams
                    WHERE league_id = @league_id",
                new { league_id = leagueId });
                query = query.ToList();

                foreach (var item in query)
                {
                    result.Add(item);
                }
            }
    
            return result;
        }

        private async Task<string> GetTeamName(int leagueId, int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamName = await conn.QueryFirstAsync<string>(
                    @"SELECT name
                    FROM double_league_teams
                    WHERE id = @id AND league_id = @league_id",
                new { id = teamId, league_id = leagueId });
                
                return teamName;
            }
        }

        private async Task<List<DoubleLeagueMatchModel>> GetMatchesWonATeamOne(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_matches
                    WHERE team_one_id = @team_one_id AND match_ended = true AND team_one_score > team_two_score",
                new { team_one_id = teamId });
                
                return matches.ToList();
            }
        }

        private async Task<List<DoubleLeagueMatchModel>> GetMatchesWonATeamTwo(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_matches
                    WHERE team_two_id = @team_two_id AND match_ended = true AND team_two_score > team_one_score",
                new { team_two_id = teamId });
                
                return matches.ToList();
            }
        }

        private async Task<List<DoubleLeagueMatchModel>> GetMatchesLostATeamOne(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_matches
                    WHERE team_one_id = @team_one_id AND match_ended = true AND team_one_score < team_two_score",
                new { team_one_id = teamId });
                
                return matches.ToList();
            }
        }

        private async Task<List<DoubleLeagueMatchModel>> GetMatchesLostATeamTwo(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_matches
                    WHERE team_two_id = @team_two_id AND match_ended = true AND team_two_score < team_one_score",
                new { team_two_id = teamId });
                
                return matches.ToList();
            }
        }

        private async Task<int> GetTotalGoalsScored(int teamId)
        {
            int result = 0;
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_goals
                    WHERE scored_by_team_id = @scored_by_team_id",
                new { scored_by_team_id = teamId });
                
                query = query.ToList();
                
                result = query.Count();
            }

            return result;
        }

        private async Task<int> GetTolalGoalsRecieved(int teamId)
        {
            int result = 0;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM double_league_goals
                    WHERE opponent_team_id = @scored_by_team_id",
                new { opponent_team_id = teamId });
                
                query = query.ToList();
                
                result = query.Count();
            }
            return result;
        }

        private async Task<List<DoubleLeaguePlayerModel>> GetDoubleLeaguePlayers(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeaguePlayerModel>(
                    @"SELECT DISTINCT id as Id, user_id as UserId, 
                    double_league_team_id as DoubleLeagueTeamId
                    FROM double_league_players
                    WHERE double_league_team_id = @double_league_team_id",
                new { double_league_team_id = teamId });
                
                query = query.ToList();

                var data = query.ToList();
                
                return data;
            }
        }

        private async Task<User> GetUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryFirstAsync<User>(
                    @"SELECT id as Id, first_name as FirstName,
                    last_name as LastName, email as Email
                    FROM users
                    WHERE id = @id",
                new { id = userId });
                
                return query;
            }
        }

        public async Task<TeamMember[]> GetTeamMembers(int teamId)
        {
            List<TeamMember> teamMembers = new();

            var players = await GetDoubleLeaguePlayers(teamId);

            foreach (var player in players)
            {
                var user = await GetUser(player.UserId);

                TeamMember teamMember = new TeamMember
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email
                };

                teamMembers.Add(teamMember);
            }

            var teamMembersAsArray = teamMembers.ToArray();

            return teamMembersAsArray;
        }

         private List<DoubleLeagueStandingsQuery> ReturnSortedLeague(List<DoubleLeagueStandingsQuery> singleLeagueStandings)
        {
            return singleLeagueStandings.OrderByDescending(x => x.Points).ToList();
        }

        private List<DoubleLeagueStandingsQuery> AddPositionInLeagueToList(List<DoubleLeagueStandingsQuery> standings)
        {
            List<DoubleLeagueStandingsQuery> result = standings;
            foreach (var item in result.Select((value, i) => new { i, value }))
            {
                item.value.PositionInLeague = item.i + 1;
            }
            return result;
        }

        public async Task<IEnumerable<DoubleLeagueStandingsQuery>> GetDoubleLeagueStandings(int leagueId)
        {
            List<DoubleLeagueStandingsQuery> standings = new();
            const int Points = 3;
            const int Zero = 0;
            List<int> teamIds = await GetAllTeamIds(leagueId);

            foreach (var teamId in teamIds)
            {
                var matchesWonAsTeamOne = await GetMatchesWonATeamOne(teamId);
                var matchesWonAsTeamTwo = await GetMatchesWonATeamTwo(teamId);

                var matchesLostAsTeamOne = await GetMatchesLostATeamOne(teamId);
                var matchesLostAsTeamTwo = await GetMatchesLostATeamTwo(teamId);

                int totalMatchesWon = matchesWonAsTeamOne.Count() + matchesWonAsTeamTwo.Count();
                int totalMatchesLost = matchesLostAsTeamOne.Count() + matchesLostAsTeamTwo.Count();
                
                DoubleLeagueStandingsQuery dls = new DoubleLeagueStandingsQuery
                {
                    TeamID = teamId,
                    LeagueId = leagueId,
                    TotalMatchesWon = totalMatchesWon,
                    TotalMatchesLost = totalMatchesLost,
                    TotalGoalsScored = await GetTotalGoalsScored(teamId),
                    TotalGoalsRecieved = await GetTolalGoalsRecieved(teamId),
                    PositionInLeague = Zero,
                    MatchesPlayed = Zero,
                    Points = Points * totalMatchesWon,
                    TeamMembers = await GetTeamMembers(teamId),
                    TeamName = await GetTeamName(leagueId, teamId)
                };
                standings.Add(dls);
            }

            var sortedLeague = ReturnSortedLeague(standings);
            var sortedLeagueWithPositions = AddPositionInLeagueToList(sortedLeague);

            return sortedLeagueWithPositions;
        }

        public async Task<List<DoubleLeagueMatchModel>> CreateDoubleLeagueMatches(int leagueId, int? howManyRounds)
        {
            // Check if howManyRounds parameter is null and retrieve it from the leagues table
            if (!howManyRounds.HasValue)
            {
                howManyRounds = await GetHowManyRoundsFromLeague(leagueId);
            }

            // Retrieve all teams for the given league
            var teams = await GetTeamsForLeague(leagueId);

            // Create double league matches
            var matches = GenerateDoubleLeagueMatches(teams, howManyRounds.Value, leagueId);

            // Insert matches into the database
            var newMatches = await InsertDoubleLeagueMatches(matches);

            return newMatches;
        }

        private async Task<int?> GetHowManyRoundsFromLeague(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT how_many_rounds FROM leagues WHERE id = @leagueId",
                    new { leagueId });

                return query;
            }
        }

        private async Task<List<DoubleLeagueTeamModel>> GetTeamsForLeague(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeagueTeamModel>(
                    @"SELECT id, name, created_at as CreatedAt, organisation_id as OrganisationId, league_id as LeagueId
                    FROM double_league_teams
                    WHERE league_id = @leagueId",
                    new { leagueId });

                return query.ToList();
            }
        }

        private List<DoubleLeagueMatchModel> GenerateDoubleLeagueMatches(List<DoubleLeagueTeamModel> teams, int howManyRounds, int leagueId)
        {
            var matches = new List<DoubleLeagueMatchModel>();

            for (int round = 1; round <= howManyRounds; round++)
            {
                for (int i = 0; i < teams.Count - 1; i++)
                {
                    for (int j = i + 1; j < teams.Count; j++)
                    {
                        var match = new DoubleLeagueMatchModel
                        {
                            TeamOneId = teams[i].Id,
                            TeamTwoId = teams[j].Id,
                            LeagueId = leagueId,
                            StartTime = null,
                            EndTime = null,
                            TeamOneScore = 0,
                            TeamTwoScore = 0,
                            MatchStarted = false,
                            MatchEnded = false,
                            MatchPaused = false
                        };

                        matches.Add(match);
                    }
                }
            }

            return matches;
        }

        private async Task<List<DoubleLeagueMatchModel>> InsertDoubleLeagueMatches(List<DoubleLeagueMatchModel> matches)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (var match in matches)
                        {
                            // Use RETURNING to get the ID of the newly inserted row
                            var insertedId = await conn.ExecuteScalarAsync<int>(
                                @"INSERT INTO double_league_matches (team_one_id, team_two_id, league_id, start_time, end_time, team_one_score, team_two_score, match_started, match_ended, match_paused)
                                VALUES (@TeamOneId, @TeamTwoId, @LeagueId, @StartTime, @EndTime, @TeamOneScore, @TeamTwoScore, @MatchStarted, @MatchEnded, @MatchPaused)
                                RETURNING id",
                                match, transaction);

                            // Update the match with the inserted ID
                            match.Id = insertedId;
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        // Handle the exception as needed (logging, rethrow, etc.)
                        throw;
                    }
                }
            }

            return matches;
        }

        public async Task<bool> CheckLeaguePermissionByOrganisationId(int leagueId, int currentOrganisationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryFirstAsync<LeagueModel>(
                    @"SELECT id, organisation_id AS OrganisationId
                    FROM leagues
                    WHERE id = @leagueId",
                    new { leagueId });

                if (query != null)
                {
                    if (query.Id == leagueId && query.OrganisationId == currentOrganisationId)
                    {
                        return true;
                    }
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeagueTeams;
using FoosballApi.Models.Other;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeaugeMatchService
    {
        Task<bool> CheckMatchAccess(int matchId, int userId, int currentOrganisationId);
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

        // context.DoubleLeagueMatches.SingleOrDefault(x => x.Id == matchId);
        private async Task<DoubleLeagueMatchModel> GetMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstOrDefaultAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @matchId",
                    new { matchId });
                return user;
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
    }
}
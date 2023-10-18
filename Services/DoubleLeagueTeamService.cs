using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueTeams;
using FoosballApi.Models.Leagues;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeagueTeamService
    {
        Task<List<DoubleLeagueTeamModel>> GetDoubleLeagueTeamsByLeagueId(int leagueId);
        Task<bool> CheckLeaguePermission(int leagueId, int userId);
        Task<bool> CheckLeaguePermissionEasy(int leagueId, int userId, int currentOrganisationId);
        DoubleLeagueTeamModel CreateDoubleLeagueTeam(int leagueId, int currentOrganisationId, string name);
        Task<bool> CheckDoubleLeagueTeamPermission(int teamId, int userId, int currentOrganisationId);
        DoubleLeagueTeamModel GetDoubleLeagueTeamById(int teamId);
        void DeleteDoubleLeagueTeam(int teamId);
    }

    public class DoubleLeagueTeamService : IDoubleLeagueTeamService
    {
        public string _connectionString { get; }

        public DoubleLeagueTeamService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }
        
        private async Task<DoubleLeagueTeamModel> GetDoubleLeagueTeamsByTeamId(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
               var query = await conn.QueryAsync<DoubleLeagueTeamModel>(
                    @"
                    SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_id as OrganisationId, league_id as LeagueId
                    FROM double_league_teams
                    WHERE id = @id",
                    new { id = teamId });
                return query.FirstOrDefault();
            }
        }

        private async Task<OrganisationListModel> GetOrganisationListByOrgIdAndUserId(int organisationId, int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
               var query = await conn.QueryAsync<OrganisationListModel>(
                    @"
                    SELECT id as Id, organisation_id as OrganisationId, user_id as UserId
                    FROM organisation_list
                    WHERE organisation_id = @organisation_id AND user_id = @user_id",
                    new { organisation_Id = organisationId, user_id = userId });
                return query.FirstOrDefault();
            }
        }

        public async Task<bool> CheckDoubleLeagueTeamPermission(int teamId, int userId, int currentOrganisationId)
        {
            var query = await GetDoubleLeagueTeamsByTeamId(teamId);

            if (query.OrganisationId != currentOrganisationId)
                return false;

            var query2 = await GetOrganisationListByOrgIdAndUserId( query.OrganisationId, userId);

            if (query2.OrganisationId == currentOrganisationId && query2.UserId == userId)
                return true;


            return false;
        }

        public async Task<List<DoubleLeagueTeamsJoinModel>> GetDoubleLeagueTeamsJoinedWithPlayers(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeagueTeamsJoinModel>(
                    @"SELECT dlp.id as Id, dlp.user_id as UserId, dlp.double_league_team_id as DoubleLeagueTeamId,
                    dlt.league_id as LeagueId
                    FROM double_league_players dlp
                    JOIN double_league_teams dlt ON dlp.double_league_team_id = dlt.id
                    WHERE dlt.league_id = @league_id",
                new { league_id = leagueId });
                return query.ToList();
            }
        }

        public async Task<bool> CheckLeaguePermission(int leagueId, int userId)
        {
            bool result = false;

            var query = await GetDoubleLeagueTeamsJoinedWithPlayers(leagueId);

            var data = query.ToList();

            foreach (var item in data)
            {
                if (item.UserId == userId)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public DoubleLeagueTeamModel CreateDoubleLeagueTeam(int leagueId, int currentOrganisationId, string name)
        {
            DateTime now = DateTime.Now;
            DoubleLeagueTeamModel newTeam = new();
            newTeam.Name = name;
            newTeam.CreatedAt = now;
            newTeam.OrganisationId = currentOrganisationId;
            newTeam.LeagueId = leagueId;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"INSERT INTO double_league_teams (name, created_at, organisation_id, league_id)
                    VALUES (@name, @created_at, @organisation_id, @league_id)",
                    new {
                        name = newTeam.Name, 
                        created_at = newTeam.CreatedAt,
                        organisation_id = newTeam.OrganisationId,
                        league_id = newTeam.LeagueId
                    });
            }

            return newTeam;
        }

        private DoubleLeagueTeamModel GetDoubleTeamByTeamId(int teamId)
        {
            // use dapper
            DoubleLeagueTeamModel result = new();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                result = conn.Query<DoubleLeagueTeamModel>(
                    @"SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_id as OrganisationId, league_id as LeagueId
                    FROM double_league_teams
                    WHERE id = @id",
                    new { id = teamId }).FirstOrDefault();
            }
            return result;
        }

        public void DeleteDoubleLeagueTeam(int teamId)
        {
            var itemToDelete = GetDoubleTeamByTeamId(teamId);

            if (itemToDelete == null)
            {
                throw new ArgumentNullException(nameof(itemToDelete));
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM double_league_teams WHERE id = @id",
                    new { id = itemToDelete.Id });
            }
        }

        public DoubleLeagueTeamModel GetDoubleLeagueTeamById(int teamId)
        {
            return GetDoubleTeamByTeamId(teamId);
        }

        public async Task<List<DoubleLeagueTeamModel>> GetDoubleLeagueTeamsByLeagueId(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<DoubleLeagueTeamModel>(
                    @"SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_id as OrganisationId, league_id as LeagueId
                    FROM double_league_teams
                    WHERE league_id = @league_id",
                    new { league_id = leagueId });
                return query.ToList();
            }
        }

        public async Task<bool> CheckLeaguePermissionEasy(int leagueId, int userId, int currentOrganisationId)
        {
            bool result = false;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<LeagueModel>(
                    @"SELECT id as Id, name as Name, organisation_id as OrganisationId
                    FROM leagues
                    WHERE organisation_id = @organisation_id",
                    new { organisation_id = currentOrganisationId });

                
                foreach (var item in query)
                {
                    if (item.Id == leagueId) 
                    {
                        result = true;
                    }
                }
                return result;
            }
        }
    }
}

using System.Collections.Generic;
using FoosballApi.Models.Leagues;
using System.Linq;
using System;
using FoosballApi.Models;
using Npgsql;
using Dapper;

namespace FoosballApi.Services
{
    public interface ILeagueService
    {
        Task<IEnumerable<LeagueModel>> GetLeaguesByOrganisationId(int organisationId);
        Task<bool> CheckLeagueAccess(int userId, int organisationId);
    }

    public class LeagueService : ILeagueService
    {
        private string _connectionString { get; }
        public LeagueService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<List<OrganisationListModel>> GetOrganisationList(int userId, int organisationId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<OrganisationListModel>(
                    @"SELECT id as Id, organisation_id as OrganisationId, user_id as UserId
                    FROM organisation_list
                    WHERE user_id = @user_id AND organisation_id = @organisation_id",
                new { user_id = userId, organisation_id = organisationId });
                return goals.ToList();
            }
        }

        public async Task<bool> CheckLeagueAccess(int userId, int organisationId)
        {
            var query = await GetOrganisationList(userId, organisationId);

            var data = query.ToList();
            if (data.Count == 0)
                return false;

            return true;
        }

        public async Task<IEnumerable<LeagueModel>> GetLeaguesByOrganisationId(int organisationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var leagues = await conn.QueryAsync<LeagueModel>(
                    @"SELECT id as Id, name as Name, type_of_league as LeagueType,
                    created_at as CreatedAt, up_to as UpTo, organisation_id as OrganisationId,
                    has_league_started as HasLeagueStarted, how_many_rounds as HowManyRounds
                    FROM leagues
                    WHERE organisation_id = @organisation_id",
                new { organisation_id = organisationId });
                return leagues.ToList();
            }
        }
    }
}

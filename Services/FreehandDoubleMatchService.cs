using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandDoubleMatchService
    {
        Task<bool> CheckMatchPermission(int userId, int matchId);
    }

    public class FreehandDoubleMatchService : IFreehandDoubleMatchService
    {

        private string _connectionString { get; }
        public FreehandDoubleMatchService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<FreehandDoubleMatchModel> GetDoubleMatchData(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var doubleMatchData = await conn.QueryFirstOrDefaultAsync<FreehandDoubleMatchModel>(
                    @"select id as Id, organisation_id as OrganisationId 
                    from freehand_double_matches 
                    where id = @id",
                    new { id = matchId });
                return doubleMatchData;
            }
        }

        private async Task<User> GetCurrentUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var doubleMatchData = await conn.QueryFirstOrDefaultAsync<User>(
                    @"select id as Id, current_organisation_id as CurrentOrganisationId 
                    from users 
                    where id = @id",
                    new { id = userId });
                return doubleMatchData;
            }
        }

        public async Task<bool> CheckMatchPermission(int userId, int matchId)
        {
            var doubleMatchData = await GetDoubleMatchData(matchId);
            
            var currentUser = await GetCurrentUser(userId);

            if (doubleMatchData.OrganisationId == currentUser.CurrentOrganisationId)
                return true;

            return false;
        }
    }
}
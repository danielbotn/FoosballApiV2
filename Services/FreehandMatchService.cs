using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandMatchService
    {
        Task<bool> CheckFreehandMatchPermission(int matchId, int userId);
    }
    public class FreehandMatchService : IFreehandMatchService
    {
        private string _connectionString { get; }
        public FreehandMatchService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<FreehandPermissionModel> GetFreehandMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<FreehandPermissionModel>(
                    @"SELECT id as MatchId, player_one_id as PlayerOneId, player_two_id as PlayerTwoId
                    FROM freehand_matches
                    WHERE id = @id",
                    new { id = matchId });
                return match;
            }
        }

        private async Task<IEnumerable<OrganisationListModel>> GetAllOrganisationsOfUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<OrganisationListModel>(
                    @"SELECT id as Id, organisation_id as OrganisationId, user_id as UserId
                    FROM organisation_list
                    WHERE user_id = @user_id",
                new { user_id = userId });
                return goals.ToList();
            }
        }

        public async Task<bool> CheckFreehandMatchPermission(int matchId, int userId)
        {
           var query = await GetFreehandMatchById(matchId);

            var queryData = query;

            if (queryData == null)
                return false;

            IEnumerable<OrganisationListModel> currentUser = await GetAllOrganisationsOfUser(userId);

            IEnumerable<OrganisationListModel> playerOne = await GetAllOrganisationsOfUser(queryData.PlayerOneId);

            IEnumerable<OrganisationListModel> playerTwo = await GetAllOrganisationsOfUser(queryData.PlayerTwoId);

            bool sameOrganisationAsPlayerOne = false;
            bool sameOrganisationAsPlayerTwo = false;

            foreach (var element in currentUser)
            {
                foreach (var p1Item in playerOne)
                {
                    if (element.OrganisationId == p1Item.OrganisationId)
                    {
                        sameOrganisationAsPlayerOne = true;
                        break;
                    }
                }

                foreach (var p2Item in playerTwo)
                {
                    if (element.OrganisationId == p2Item.OrganisationId)
                    {
                        sameOrganisationAsPlayerTwo = true;
                        break;
                    }
                }
            }

            // User has permissions if both players belong to same organisation
            if (sameOrganisationAsPlayerOne && sameOrganisationAsPlayerTwo)
                return true;

            return false;
        }
    }
}
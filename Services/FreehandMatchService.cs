using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandMatchService
    {
        Task<bool> CheckFreehandMatchPermission(int matchId, int userId);
        Task<IEnumerable<FreehandMatchModelExtended>> GetAllFreehandMatches(int userId);
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

        private async Task<List<FreehandMatchModel>> GetFreehandMatchesByUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<FreehandMatchModel>(
                    @"SELECT id as Id, player_one_id as PlayerOneId, player_two_id as PlayerTwoId, 
                    start_time as StartTime, end_time as EndTime, player_one_score as PlayerOneScore,
                    player_two_score as PlayerTwoScore, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused, organisation_id as OrganisationId
                    FROM freehand_matches
                    WHERE player_one_id = @user_id OR player_two_id = @user_id",
                new { user_id = userId });
                return goals.ToList();
            }
        }

        private async Task<string> GetFirstNameOfUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<string>(
                    @"SELECT first_name as FirstName
                    FROM users
                    WHERE id = @userId",
                    new { userId });
                return data;
            }
        }

        private async Task<string> GetLastNameOfUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<string>(
                    @"SELECT last_name as LastName
                    FROM users
                    WHERE id = @userId",
                    new { userId });
                return data;
            }
        }

        private async Task<string> GetPhotoUrlOfUser(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<string>(
                    @"SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @userId",
                    new { userId });
                return data;
            }
        }

        private string ToReadableAgeString(TimeSpan span)
        {
            return string.Format("{0:hh\\:mm\\:ss}", span);
        }

        public async Task<IEnumerable<FreehandMatchModelExtended>> GetAllFreehandMatches(int userId)
        {
            var data = await GetFreehandMatchesByUser(userId);

            
            List<FreehandMatchModelExtended> freehandMatchModelExtendedList = new List<FreehandMatchModelExtended>();

            foreach (var item in data)
            {
                TimeSpan? playingTime = null;
                if (item.EndTime != null) {
                    playingTime = item.EndTime - item.StartTime;
                }
                FreehandMatchModelExtended fmme = new FreehandMatchModelExtended
                {
                    Id = item.Id,
                    PlayerOneId = item.PlayerOneId,
                    PlayerOneFirstName = await GetFirstNameOfUser(item.PlayerOneId),
                    PlayerOneLastName = await GetLastNameOfUser(item.PlayerOneId),
                    PlayerOnePhotoUrl = await GetPhotoUrlOfUser(item.PlayerOneId),
                    PlayerTwoId = item.PlayerTwoId,
                    PlayerTwoFirstName = await GetFirstNameOfUser(item.PlayerTwoId),
                    PlayerTwoLastName = await GetLastNameOfUser(item.PlayerTwoId),
                    PlayerTwoPhotoUrl = await GetPhotoUrlOfUser(item.PlayerTwoId),
                    StartTime = item.StartTime,
                    EndTime = item.EndTime,
                    PlayerOneScore = item.PlayerOneScore,
                    PlayerTwoScore = item.PlayerTwoScore,
                    UpTo = item.UpTo,
                    GameFinished = item.GameFinished,
                    GamePaused = item.GamePaused,
                    TotalPlayingTime = playingTime != null ? ToReadableAgeString(playingTime.Value) : null,
                };
                freehandMatchModelExtendedList.Add(fmme);
            }

            return freehandMatchModelExtendedList;
        }
    }
}
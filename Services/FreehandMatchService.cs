using Dapper;
using FoosballApi.Dtos.Matches;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandMatchService
    {
        Task<bool> CheckFreehandMatchPermission(int matchId, int userId);
        Task<IEnumerable<FreehandMatchModelExtended>> GetAllFreehandMatches(int userId);
        Task<IEnumerable<FreehandMatchModelExtended>> GetFreehandMatchesByOrganisationId(int organisationId);
        Task<FreehandMatchModelExtended> GetFreehandMatchById(int matchId);
        Task<FreehandMatchModel> CreateFreehandMatch(int userId, int organisationId, FreehandMatchCreateDto freehandMatchCreateDto);
        Task<FreehandMatchModel> GetFreehandMatchByIdFromDatabase(int matchId);
        void UpdateFreehandMatch(FreehandMatchModel freehandMatchModel);
        void DeleteFreehandMatch(FreehandMatchModel freehandMatchModel);
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

        private async Task<FreehandPermissionModel> GetFreehandMatchByIdPermission(int matchId)
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
           var query = await GetFreehandMatchByIdPermission(matchId);

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

        private async Task<List<FreehandMatchModel>> GetFreehandMatchesByOrganisation(int organisationId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var matches = await conn.QueryAsync<FreehandMatchModel>(
                @"SELECT id as Id, player_one_id as PlayerOneId, player_two_id as PlayerTwoId, 
                    start_time as StartTime, end_time as EndTime, player_one_score as PlayerOneScore,
                    player_two_score as PlayerTwoScore, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused, organisation_id as OrganisationId
                    FROM freehand_matches
                    WHERE organisation_id = @organisation_id AND game_finished = false",
            new { organisation_id = organisationId });
            return matches.ToList();
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

        private async Task<FreehandMatchModel> GetMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<FreehandMatchModel>(
                    @"SELECT id as Id, player_one_id as PlayerOneId, player_two_id as PlayerTwoId, 
                    start_time as StartTime, end_time as EndTime, player_one_score as PlayerOneScore,
                    player_two_score as PlayerTwoScore, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused, organisation_id as OrganisationId
                    FROM freehand_matches
                    WHERE id = @id",
                    new { id = matchId });
                return match;
            }
        }

        public async Task<FreehandMatchModelExtended> GetFreehandMatchById(int matchId)
        {
            var data = await GetMatchById(matchId);
            TimeSpan? playingTime = null;
            if (data.EndTime != null) {
                playingTime = data.EndTime - data.StartTime;
            }
            FreehandMatchModelExtended fmme = new FreehandMatchModelExtended
            {
                Id = data.Id,
                PlayerOneId = data.PlayerOneId,
                PlayerOneFirstName = await GetFirstNameOfUser(data.PlayerOneId),
                PlayerOneLastName = await GetLastNameOfUser(data.PlayerOneId),
                PlayerOnePhotoUrl = await GetPhotoUrlOfUser(data.PlayerOneId),
                PlayerTwoId = data.PlayerTwoId,
                PlayerTwoFirstName = await GetFirstNameOfUser(data.PlayerTwoId),
                PlayerTwoLastName = await GetLastNameOfUser(data.PlayerTwoId),
                PlayerTwoPhotoUrl = await GetPhotoUrlOfUser(data.PlayerTwoId),
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                TotalPlayingTime = playingTime != null ? ToReadableAgeString(playingTime.Value) : null,
                PlayerOneScore = data.PlayerOneScore,
                PlayerTwoScore = data.PlayerTwoScore,
                UpTo = data.UpTo,
                GameFinished = data.GameFinished,
                GamePaused = data.GamePaused,
            };
            return fmme;
        }

        public async Task<FreehandMatchModel> CreateFreehandMatch(int userId, int organisationId, FreehandMatchCreateDto freehandMatchCreateDto)
        {
            FreehandMatchModel fmm = new FreehandMatchModel();
            DateTime now = DateTime.Now;
            fmm.PlayerOneId = freehandMatchCreateDto.PlayerOneId;
            fmm.PlayerTwoId = freehandMatchCreateDto.PlayerTwoId;
            fmm.PlayerOneScore = freehandMatchCreateDto.PlayerOneScore;
            fmm.PlayerTwoScore = freehandMatchCreateDto.PlayerTwoScore;
            fmm.StartTime = now;
            fmm.GameFinished = freehandMatchCreateDto.GameFinished;
            fmm.GamePaused = freehandMatchCreateDto.GamePaused;
            fmm.UpTo = freehandMatchCreateDto.UpTo;
            fmm.OrganisationId = organisationId;
           
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<FreehandMatchModel>(
                    @"INSERT INTO freehand_matches (player_one_id, player_two_id, player_one_score, 
                    player_two_score, start_time, game_finished, game_paused, up_to, organisation_id)
                    VALUES (@playerOneId, @playerTwoId, @playerOneScore, @playerTwoScore, @startTime, @gameFinished, @gamePaused, @upTo, @organisationId)
                    RETURNING id",
                    new 
                    { 
                        playerOneId = fmm.PlayerOneId, 
                        playerTwoId = fmm.PlayerTwoId, 
                        playerOneScore = fmm.PlayerOneScore, 
                        playerTwoScore = fmm.PlayerTwoScore, 
                        startTime = fmm.StartTime,
                        gameFinished = fmm.GameFinished, 
                        gamePaused = fmm.GamePaused, 
                        upTo = fmm.UpTo, 
                        organisationId = fmm.OrganisationId 
                    });
                fmm.Id = data.Id;
            }

            return fmm;
        }

        public async Task<FreehandMatchModel> GetFreehandMatchByIdFromDatabase(int matchId)
        {
            var data = await GetMatchById(matchId);
            return data;
        }

        public void UpdateFreehandMatch(FreehandMatchModel freehandMatchModel)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE freehand_matches
                    SET player_one_id = @player_one_id, player_two_id = @player_two_id, player_one_score = @player_one_score, 
                    player_two_score = @player_two_score, start_time = @start_time, end_time = @end_time, game_finished = @game_finished, 
                    game_paused = @game_paused, up_to = @up_to, organisation_id = @organisation_id
                    WHERE id = @id",
                    new
                    {
                        id = freehandMatchModel.Id,
                        player_one_id = freehandMatchModel.PlayerOneId,
                        player_two_id = freehandMatchModel.PlayerTwoId,
                        player_one_score = freehandMatchModel.PlayerOneScore,
                        player_two_score = freehandMatchModel.PlayerTwoScore,
                        start_time = freehandMatchModel.StartTime,
                        end_time = freehandMatchModel.EndTime,
                        game_finished = freehandMatchModel.GameFinished,
                        game_paused = freehandMatchModel.GamePaused,
                        up_to = freehandMatchModel.UpTo,
                        organisation_id = freehandMatchModel.OrganisationId
                    });
            }
        }

        public void DeleteFreehandMatch(FreehandMatchModel freehandMatchModel)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"DELETE FROM freehand_matches
                    WHERE id = @id",
                    new
                    {
                        id = freehandMatchModel.Id
                    });
            }
        }

        public async Task<IEnumerable<FreehandMatchModelExtended>> GetFreehandMatchesByOrganisationId(int organisationId)
        {
            var data = await GetFreehandMatchesByOrganisation(organisationId);

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
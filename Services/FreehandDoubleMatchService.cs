using Dapper;
using FoosballApi.Dtos.DoubleMatches;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandDoubleMatchService
    {
        Task<bool> CheckMatchPermission(int userId, int matchId);
        Task<IEnumerable<FreehandDoubleMatchModel>> GetAllFreehandDoubleMatches(int userId);
        Task<FreehandDoubleMatchModelExtended> GetFreehandDoubleMatchByIdExtended(int matchId);
        Task<FreehandDoubleMatchModel> CreateFreehandDoubleMatch(int userId, FreehandDoubleMatchCreateDto freehandDoubleMatchCreateDto);
        Task<FreehandDoubleMatchModel> GetFreehandDoubleMatchById(int matchId);
        void UpdateFreehandMatch(FreehandDoubleMatchModel freehandMatchModel);
        void DeleteFreehandMatch(FreehandDoubleMatchModel freehandDoubleMatchModel);
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

        public async Task<IEnumerable<FreehandDoubleMatchModel>> GetAllFreehandDoubleMatches(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<FreehandDoubleMatchModel>(
                    @"SELECT id as Id, player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA,
                    player_one_team_b as PlayerOneTeamB, player_two_team_b as PlayerTwoTeamB,
                    organisation_id as OrganisationId, start_time as StartTime, end_time as EndTime,
                    team_a_score as TeamAScore, team_b_score as TeamBScore, nickname_team_a as NicknameTeamA,
                    nickname_team_b as NicknameTeamB, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused
                    FROM freehand_double_matches
                    WHERE player_one_team_a = @userId OR player_two_team_a = @userId OR
                    player_one_team_b = @userId OR player_two_team_b = @userId
                    ",
                new { userId });
                return matches.ToList();
            }
        }

        public async Task<FreehandDoubleMatchModel> GetFreehandDoubleMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<FreehandDoubleMatchModel>(
                    @"SELECT id as Id, player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA,
                    player_one_team_b as PlayerOneTeamB, player_two_team_b as PlayerTwoTeamB,
                    organisation_id as OrganisationId, start_time as StartTime, end_time as EndTime,
                    team_a_score as TeamAScore, team_b_score as TeamBScore, nickname_team_a as NicknameTeamA,
                    nickname_team_b as NicknameTeamB, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused
                    FROM freehand_double_matches
                    WHERE id = @id",
                    new { id = matchId });
                return data;
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

        public async Task<FreehandDoubleMatchModelExtended> GetFreehandDoubleMatchByIdExtended(int matchId)
        {
            var data = await GetFreehandDoubleMatchById(matchId);
            TimeSpan? playingTime = null;
            if (data.EndTime != null) {
                playingTime = data.EndTime - data.StartTime;
            }
            FreehandDoubleMatchModelExtended fdme = new FreehandDoubleMatchModelExtended {
                Id = data.Id,
                PlayerOneTeamA = data.PlayerOneTeamA,
                PlayerOneTeamAFirstName = await GetFirstNameOfUser(data.PlayerOneTeamA),
                PlayerOneTeamALastName = await GetLastNameOfUser(data.PlayerOneTeamA),
                PlayerOneTeamAPhotoUrl = await GetPhotoUrlOfUser(data.PlayerOneTeamA),
                PlayerTwoTeamA = data.PlayerTwoTeamA,
                PlayerTwoTeamAFirstName = data.PlayerTwoTeamA != null ? await GetFirstNameOfUser((int)data.PlayerTwoTeamA) : null,
                PlayerTwoTeamALastName = data.PlayerTwoTeamA != null ? await GetLastNameOfUser((int)data.PlayerTwoTeamA) : null,
                PlayerTwoTeamAPhotoUrl = data.PlayerTwoTeamA != null ? await GetPhotoUrlOfUser((int)data.PlayerTwoTeamA) : null,
                PlayerOneTeamB = data.PlayerOneTeamB,
                PlayerOneTeamBFirstName = await GetFirstNameOfUser(data.PlayerOneTeamB),
                PlayerOneTeamBLastName = await GetLastNameOfUser(data.PlayerOneTeamB),
                PlayerOneTeamBPhotoUrl = await GetPhotoUrlOfUser(data.PlayerOneTeamB),
                PlayerTwoTeamB = data.PlayerTwoTeamB,
                PlayerTwoTeamBFirstName = data.PlayerTwoTeamB != null ? await GetFirstNameOfUser((int)data.PlayerTwoTeamB) : null,
                PlayerTwoTeamBLastName = data.PlayerTwoTeamB != null ? await GetLastNameOfUser((int)data.PlayerTwoTeamB) : null,
                PlayerTwoTeamBPhotoUrl = data.PlayerTwoTeamB != null ? await GetPhotoUrlOfUser((int)data.PlayerTwoTeamB) : null,
                OrganisationId = data.OrganisationId,
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                TotalPlayingTime = playingTime != null ? ToReadableAgeString(playingTime.Value) : null,
                TeamAScore = data.TeamAScore,
                TeamBScore = data.TeamBScore,
                NicknameTeamA = data.NicknameTeamA,
                NicknameTeamB = data.NicknameTeamB,
                UpTo = data.UpTo,
                GameFinished = data.GameFinished,
                GamePaused = data.GamePaused
            };
            return fdme;
        }

        private async Task<FreehandDoubleMatchModel> InsertNewFreehandDoubleMatch(FreehandDoubleMatchModel fdm)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<FreehandDoubleMatchModel>(
                    @"INSERT INTO freehand_double_matches (player_one_team_a, player_two_team_a, player_one_team_b, player_two_team_b, organisation_id, start_time, end_time, team_a_score, team_b_score, nickname_team_a, nickname_team_b, up_to, game_finished, game_paused)
                    VALUES (@playerOneTeamA, @playerTwoTeamA, @playerOneTeamB, @playerTwoTeamB, @organisationId, @startTime, @endTime, @teamAScore, @teamBScore, @nicknameTeamA, @nicknameTeamB, @upTo, @gameFinished, @gamePaused)
                    RETURNING id as Id, player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA,
                    player_one_team_b as PlayerOneTeamB, player_two_team_b as PlayerTwoTeamB,
                    organisation_id as OrganisationId, start_time as StartTime, end_time as EndTime,
                    team_a_score as TeamAScore, team_b_score as TeamBScore, nickname_team_a as NicknameTeamA,
                    nickname_team_b as NicknameTeamB, up_to as UpTo, game_finished as GameFinished,
                    game_paused as GamePaused
                    ",
                    new { 
                        fdm.PlayerOneTeamA, 
                        fdm.PlayerTwoTeamA, 
                        fdm.PlayerOneTeamB, 
                        fdm.PlayerTwoTeamB, 
                        fdm.OrganisationId, 
                        fdm.StartTime, 
                        fdm.EndTime, 
                        fdm.TeamAScore, 
                        fdm.TeamBScore, 
                        fdm.NicknameTeamA, 
                        fdm.NicknameTeamB, 
                        fdm.UpTo, 
                        fdm.GameFinished, 
                        fdm.GamePaused 
                    });
                return data;
            }
        }

        public async Task<FreehandDoubleMatchModel> CreateFreehandDoubleMatch(int userId, FreehandDoubleMatchCreateDto freehandDoubleMatchCreateDto)
        {
            FreehandDoubleMatchModel fdm = new FreehandDoubleMatchModel();
            DateTime now = DateTime.Now;
            fdm.OrganisationId = freehandDoubleMatchCreateDto.OrganisationId;
            fdm.PlayerOneTeamA = freehandDoubleMatchCreateDto.PlayerOneTeamA;
            fdm.PlayerOneTeamB = freehandDoubleMatchCreateDto.PlayerOneTeamB;
            fdm.PlayerTwoTeamA = freehandDoubleMatchCreateDto.PlayerTwoTeamA;
            fdm.PlayerTwoTeamB = freehandDoubleMatchCreateDto.PlayerTwoTeamB;
            fdm.StartTime = now;
            fdm.EndTime = null;
            fdm.TeamAScore = freehandDoubleMatchCreateDto.TeamAScore;
            fdm.TeamBScore = freehandDoubleMatchCreateDto.TeamBScore;

            if (!string.IsNullOrEmpty(freehandDoubleMatchCreateDto.NicknameTeamA))
                fdm.NicknameTeamA = freehandDoubleMatchCreateDto.NicknameTeamA;

            if (!string.IsNullOrEmpty(freehandDoubleMatchCreateDto.NicknameTeamB))
                fdm.NicknameTeamB = freehandDoubleMatchCreateDto.NicknameTeamB;

            if (freehandDoubleMatchCreateDto.UpTo != null)
                fdm.UpTo = freehandDoubleMatchCreateDto.UpTo;

            fdm.GameFinished = false;
            fdm.GamePaused = false;

            var result = await InsertNewFreehandDoubleMatch(fdm); 
            return result;
        }

        public void UpdateFreehandMatch(FreehandDoubleMatchModel freehandMatchModel)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE freehand_double_matches
                    SET player_one_team_a = @player_one_team_a, 
                    player_two_team_a = @player_two_team_a, 
                    player_one_team_b = @player_one_team_b, 
                    player_two_team_b = @player_two_team_b, 
                    organisation_id = @organisation_id, 
                    start_time = @start_time,
                    end_time = @end_time,
                    team_a_score = @team_a_score,
                    team_b_score = @team_b_score,
                    nickname_team_a = @nickname_team_a,
                    nickname_team_b = @nickname_team_b,
                    up_to = @up_to,
                    game_finished = @game_finished,
                    game_paused = @game_paused
                    WHERE id = @id",
                   
                    new { 
                        player_one_team_a = freehandMatchModel.PlayerOneTeamA,
                        player_two_team_a = freehandMatchModel.PlayerTwoTeamA,
                        player_one_team_b = freehandMatchModel.PlayerOneTeamB,
                        player_two_team_b = freehandMatchModel.PlayerTwoTeamB,
                        organisation_id = freehandMatchModel.OrganisationId,
                        start_time = freehandMatchModel.StartTime,
                        end_time = freehandMatchModel.EndTime,
                        team_a_score = freehandMatchModel.TeamAScore,
                        team_b_score = freehandMatchModel.TeamBScore,
                        nickname_team_a = freehandMatchModel.NicknameTeamA,
                        nickname_team_b = freehandMatchModel.NicknameTeamB,
                        up_to = freehandMatchModel.UpTo,
                        game_finished = freehandMatchModel.GameFinished,
                        game_paused = freehandMatchModel.GamePaused,
                        id = freehandMatchModel.Id
                     });
            }
        }

        public void DeleteFreehandMatch(FreehandDoubleMatchModel freehandDoubleMatchModel)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"DELETE FROM freehand_double_matches
                    WHERE id = @id",
                    new { id = freehandDoubleMatchModel.Id });
            }
        }
    }
}
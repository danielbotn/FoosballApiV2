using Dapper;
using FoosballApi.Models.Goals;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandGoalService
    {
        Task<IEnumerable<FreehandGoalModelExtended>> GetFreehandGoalsByMatchId(int matchId, int userId);
    }
    public class FreehandGoalService : IFreehandGoalService
    {
        public string _connectionString { get; }

        public FreehandGoalService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<IEnumerable<FreehandGoalModel>> GetFreehandGoalsByMatchId(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goal = await conn.QueryAsync<FreehandGoalModel>(
                    @"SELECT id as Id, time_of_goal as TimeOfGoal, match_id as MatchId,
                    scored_by_user_id as ScoredByUserId, Oponent_id as OponentId,
                    scored_by_score as ScoredByScore, oponent_score as OponentScore,
                    winner_goal as WinnerGoal
                    FROM freehand_goals
                    WHERE match_id = @match_id
                    ORDER BY id, time_of_goal",
                    new { match_id = matchId });
                return goal;
            }
        }

        private async Task<FreehandMatchModel> GetFreehandMatchById(int matchId)
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

        private async Task<string> CalculateGoalTimeStopWatch(DateTime timeOfGoal, int matchId)
        {
            var match = await GetFreehandMatchById(matchId);
            DateTime? matchStarted = match.StartTime;
            if (matchStarted == null)
            {
                matchStarted = DateTime.Now;
            }
            TimeSpan timeSpan = matchStarted.Value - timeOfGoal;
            string result = timeSpan.ToString(@"hh\:mm\:ss");
            string sub = result.Substring(0, 2);
            // remove first two characters if they are "00:"
            if (sub == "00")
            {
                result = result.Substring(3);
            }
            return result;
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


        public async Task<IEnumerable<FreehandGoalModelExtended>> GetFreehandGoalsByMatchId(int matchId, int userId)
        {
            List<FreehandGoalModelExtended> result = new List<FreehandGoalModelExtended>();
            var query = await GetFreehandGoalsByMatchId(matchId);
            var data = query.ToList();

            foreach (var item in data)
            {
                FreehandGoalModelExtended fgme = new FreehandGoalModelExtended
                {
                    Id = item.Id,
                    TimeOfGoal = item.TimeOfGoal,
                    GoalTimeStopWatch = await CalculateGoalTimeStopWatch(item.TimeOfGoal, item.MatchId),
                    MatchId = item.MatchId,
                    ScoredByUserId = item.ScoredByUserId,
                    ScoredByUserFirstName = await GetFirstNameOfUser(item.ScoredByUserId),
                    ScoredByUserLastName = await GetLastNameOfUser(item.ScoredByUserId),
                    ScoredByUserPhotoUrl = await GetPhotoUrlOfUser(item.ScoredByUserId),
                    OponentId = item.OponentId,
                    OponentFirstName = await GetFirstNameOfUser(item.OponentId),
                    OponentLastName = await GetLastNameOfUser(item.OponentId),
                    OponentPhotoUrl = await GetPhotoUrlOfUser(item.OponentId),
                    ScoredByScore = item.ScoredByScore,
                    OponentScore = item.OponentScore,
                    WinnerGoal = item.WinnerGoal
                };
                result.Add(fgme);
            }

            return result;
        }
    }
}
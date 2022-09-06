using Dapper;
using FoosballApi.Models.Matches;
using FoosballApi.Models.SingleLeagueGoals;
using Npgsql;

namespace FoosballApi.Services
{
    public interface ISingleLeagueGoalService
    {
        Task<IEnumerable<SingleLeagueGoalModelExtended>> GetAllSingleLeagueGoalsByMatchId(int matchId);
        bool CheckSingleLeagueGoalPermission(int userId, int goalId, int organisationId);
        bool CheckCreatePermission(int userId, SingleLeagueCreateModel singleLeagueCreateModel);
        SingleLeagueGoalModel GetSingleLeagueGoalById(int goaldId);
        void DeleteSingleLeagueGoal(SingleLeagueGoalModel singleLeagueGoalModel);
        SingleLeagueGoalModel CreateSingleLeagueGoal(SingleLeagueCreateModel singleLeagueCreateMode);
    }

    public class SingleLeagueGoalService : ISingleLeagueGoalService
    {
        private string _connectionString { get; }
        public SingleLeagueGoalService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public bool CheckCreatePermission(int userId, SingleLeagueCreateModel singleLeagueCreateModel)
        {
            throw new NotImplementedException();
        }

        public bool CheckSingleLeagueGoalPermission(int userId, int goalId, int organisationId)
        {
            throw new NotImplementedException();
        }

        public SingleLeagueGoalModel CreateSingleLeagueGoal(SingleLeagueCreateModel singleLeagueCreateMode)
        {
            throw new NotImplementedException();
        }

        public void DeleteSingleLeagueGoal(SingleLeagueGoalModel singleLeagueGoalModel)
        {
            throw new NotImplementedException();
        }

        private async Task<List<SingleLeagueGoalModel>> GetSingleLeagueGoalsByMatchIdAsList(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<SingleLeagueGoalModel>(
                    @"SELECT id as Id, time_of_goal as TimeOfGoal, match_id as MatchId,
                    scored_by_user_id as ScoredByUserId, opponent_id as OpponentId,
                    scorer_score as ScorerScore, opponent_score as OpponentScore,
                    winner_goal as WinnerGoal
                    FROM single_league_goals
                    WHERE match_id = @match_id",
                new { match_id = matchId });
                
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

        private async Task<SingleLeagueMatchModel> GetSingleLeagueMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id, player_one as PlayerOne, player_two as PlayerTwo,
                    league_id as LeagueId, start_time as StartTime, end_time as EndTime,
                    player_one_score as PlayerOneScore, player_two_score as PlayerTwoScore,
                    match_started as MatchStarted, match_ended as MatchEnded,
                    match_paused as MatchPaused
                    FROM single_league_matches
                    WHERE id = @id",
                    new { id = matchId });
                return data;
            }
        }

        private async Task<string> CalculateGoalTimeStopWatch(DateTime timeOfGoal, int matchId)
        {
            var match = await GetSingleLeagueMatchById(matchId);
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

        public async Task<IEnumerable<SingleLeagueGoalModelExtended>> GetAllSingleLeagueGoalsByMatchId(int matchId)
        {
            var query = await GetSingleLeagueGoalsByMatchIdAsList(matchId);
            
            List<SingleLeagueGoalModelExtended> slgmList = new List<SingleLeagueGoalModelExtended>();
            foreach (var slgm in query)
            {
                var slgmExtended = new SingleLeagueGoalModelExtended
                {
                    Id = slgm.Id,
                    TimeOfGoal = slgm.TimeOfGoal,
                    MatchId = slgm.MatchId,
                    ScoredByUserId = slgm.ScoredByUserId,
                    ScoredByUserFirstName = await GetFirstNameOfUser(slgm.ScoredByUserId),
                    ScoredByUserLastName = await GetLastNameOfUser(slgm.ScoredByUserId),
                    ScoredByUserPhotoUrl = await GetPhotoUrlOfUser(slgm.ScoredByUserId),
                    OpponentId = slgm.OpponentId,
                    OpponentFirstName = await GetFirstNameOfUser(slgm.OpponentId),
                    OpponentLastName = await GetLastNameOfUser(slgm.OpponentId),
                    OpponentPhotoUrl = await GetPhotoUrlOfUser(slgm.OpponentId),
                    ScorerScore = slgm.ScorerScore,
                    OpponentScore = slgm.OpponentScore,
                    WinnerGoal = slgm.WinnerGoal,
                    GoalTimeStopWatch = await CalculateGoalTimeStopWatch(slgm.TimeOfGoal, slgm.MatchId),
                };
                slgmList.Add(slgmExtended);
            }
            return slgmList;
        }

        public SingleLeagueGoalModel GetSingleLeagueGoalById(int goaldId)
        {
            throw new NotImplementedException();
        }
    }
}
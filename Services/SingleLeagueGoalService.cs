using Dapper;
using FoosballApi.Models.Leagues;
using FoosballApi.Models.Matches;
using FoosballApi.Models.SingleLeagueGoals;
using Npgsql;

namespace FoosballApi.Services
{
    public interface ISingleLeagueGoalService
    {
        Task<IEnumerable<SingleLeagueGoalModelExtended>> GetAllSingleLeagueGoalsByMatchId(int matchId);
        Task<bool> CheckSingleLeagueGoalPermission(int userId, int goalId, int organisationId);
        bool CheckCreatePermission(int userId, SingleLeagueCreateModel singleLeagueCreateModel);
        Task<SingleLeagueGoalModelExtended> GetSingleLeagueGoalById(int goaldId);
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

        private async Task<SingleLeagueGoalModel> GetGoalQuery(int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goal = await conn.QueryFirstAsync<SingleLeagueGoalModel>(
                    @"SELECT id as Id, time_of_goal as TimeOfGoal, match_id as MatchId,
                    scored_by_user_id as ScoredByUserId, opponent_id as OpponentId,
                    scorer_score as ScorerScore, opponent_score as OpponentScore,
                    winner_goal as WinnerGoal
                    FROM single_league_goals
                    WHERE id = @id",
                new { id = goalId });
                
                return goal;
            }
        }

        private async Task<SingleLeagueMatchModel > GetMatchQuery(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstAsync<SingleLeagueMatchModel >(
                    @"SELECT id as Id, player_one as PlayerOne, player_two as PlayerTwo,
                    league_id as LeagueId, start_time as StartTime, end_time as EndTime,
                    player_one_score as PlayerOneScore, player_two_score as PlayerTwoScore,
                    match_started as MatchStarted, match_ended as MatchEnded, match_paused as
                    MatchPaused
                    FROM single_league_matches
                    WHERE id = @id",
                new { id = matchId });
                
                return match;
            }
        }

        private async Task<List<LeaguePlayersModel>> GetLeaguePlayersQuery(int userId, int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var players = await conn.QueryAsync<LeaguePlayersModel >(
                    @"SELECT id as Id, user_id as UserId, league_id as LeagueId,
                    created_at as CreatedAt
                    FROM league_players
                    WHERE user_id = @user_id AND league_id = @league_id",
                new { user_id = userId, league_id = leagueId });
                return players.ToList();
            }
        }

        public async Task<bool> CheckSingleLeagueGoalPermission(int userId, int goalId, int organisationId)
        {
            bool result = false;

            var goalQuery = await GetGoalQuery(goalId);

            int matchId = goalQuery.MatchId;

            var matchQuery = await GetMatchQuery(matchId);

            int leaguId = matchQuery.LeagueId;

            var leaguePlayersQuery = await GetLeaguePlayersQuery(userId, leaguId);

            foreach (var lp in leaguePlayersQuery)
            {
                if (lp.UserId == userId)
                {
                    result = true;
                    break;
                }
            }

            return result;
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

        public async Task<SingleLeagueGoalModelExtended> GetSingleLeagueGoalById(int goaldId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<SingleLeagueGoalModelExtended>(
                    @"SELECT slg.id as Id, slg.time_of_goal as TimeOfGoal, slg.match_id as MatchId,
                    slg.scored_by_user_id as ScoredByUserId, slg.opponent_id as OpponentId, 
                    slg.scorer_score as ScorerScore, slg.opponent_score as OpponentScore,
                    slg.winner_goal as WinnerGoal, u.first_name as ScoredByUserFirstName, u.last_name as ScoredByUserLastName,
					u.photo_url as ScoredByUserPhotoUrl,
					(SELECT uu.first_name from users uu where uu.id = slg.opponent_id) as OpponentFirstName,
					(SELECT uu.last_name from users uu where uu.id = slg.opponent_id) as OpponentLastName,
					(SELECT uu.photo_url from users uu where uu.id = slg.opponent_id) as OpponentPhotoUrl
                    FROM single_league_goals slg
					JOIN users u on u.id = slg.scored_by_user_id
                    WHERE slg.id = @id",
                    new { id = goaldId });
                return data;
            }
        }
    }
}
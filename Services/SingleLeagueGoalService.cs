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
        void DeleteSingleLeagueGoal(SingleLeagueGoalModelExtended singleLeagueGoalModel);
        Task<SingleLeagueGoalModel> CreateSingleLeagueGoal(SingleLeagueCreateModel singleLeagueCreateMode);
        Task UpdateSingleLeagueMatch(SingleLeagueGoalModel goal);
        Task UpdateSingleLeagueMatchAfterDeletedGoal(SingleLeagueGoalModelExtended singleLeagueGoalModel);
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
            bool result = false;

            if (userId == singleLeagueCreateModel.ScoredByUserId || userId == singleLeagueCreateModel.OpponentId)
                result = true;

            return result;
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

        public async Task<SingleLeagueGoalModel> CreateSingleLeagueGoal(SingleLeagueCreateModel singleLeagueCreateMode)
        {
            DateTime now = DateTime.Now;
            if (singleLeagueCreateMode == null)
            {
                throw new ArgumentNullException(nameof(singleLeagueCreateMode));
            }

            SingleLeagueGoalModel newGoal = new();
            newGoal.TimeOfGoal = now;
            newGoal.MatchId = singleLeagueCreateMode.MatchId;
            newGoal.ScoredByUserId = singleLeagueCreateMode.ScoredByUserId;
            newGoal.OpponentId = singleLeagueCreateMode.OpponentId;
            newGoal.ScorerScore = singleLeagueCreateMode.ScorerScore;
            newGoal.OpponentScore = singleLeagueCreateMode.OpponentScore;
            newGoal.WinnerGoal = singleLeagueCreateMode.WinnerGoal;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<int>(
                    @"INSERT INTO single_league_goals (time_of_goal, match_id, scored_by_user_id, 
                    opponent_id, scorer_score, opponent_score, winner_goal)
                    VALUES (@time_of_goal, @match_id, @scored_by_user_id, @opponent_id, @scorer_score, @opponent_score, @winner_goal)
                    RETURNING id",
                    new 
                    { 
                        time_of_goal = now, 
                        match_id = singleLeagueCreateMode.MatchId, 
                        scored_by_user_id = singleLeagueCreateMode.ScoredByUserId, 
                        opponent_id = singleLeagueCreateMode.OpponentId,
                        scorer_score = singleLeagueCreateMode.ScorerScore,
                        opponent_score = singleLeagueCreateMode.OpponentScore, 
                        winner_goal = singleLeagueCreateMode.WinnerGoal
                    });
                newGoal.Id = data;
            }

            return newGoal;
        }

        public void DeleteSingleLeagueGoal(SingleLeagueGoalModelExtended singleLeagueGoalModel)
        {
            if (singleLeagueGoalModel == null)
            {
                throw new ArgumentNullException(nameof(singleLeagueGoalModel));
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM single_league_goals WHERE id = @id",
                    new { id = singleLeagueGoalModel.Id });
            }
        }

        public async Task UpdateSingleLeagueMatchAfterDeletedGoal(SingleLeagueGoalModelExtended deletedGoal)
        {
            SingleLeagueMatchModel match = await GetSingleLeagueMatchById(deletedGoal.MatchId);
            
            if (deletedGoal.ScoredByUserId == match.PlayerOne && match.PlayerOneScore > 0) 
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    await conn.ExecuteAsync(
                        @"UPDATE single_league_matches
                        SET player_one_score = @player_one_score
                        WHERE id = @id",
                        new 
                        {   player_one_score = match.PlayerOneScore - 1, 
                            id = deletedGoal.MatchId
                        });
                }
            }
            else
            {
                if (match.PlayerTwoScore > 0) 
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        await conn.ExecuteAsync(
                            @"UPDATE single_league_matches
                            SET player_two_score = @player_two_score
                            WHERE id = @id",
                            new 
                            {   player_two_score = match.PlayerTwoScore - 1, 
                                id = deletedGoal.MatchId
                            });
                    }
                }
            }
        }

        public async Task UpdateSingleLeagueMatch(SingleLeagueGoalModel goal)
        {
            SingleLeagueMatchModel match = await GetSingleLeagueMatchById(goal.MatchId);

            int playerOneScore = GetPlayerOneScore(goal, match);
            int playerTwoScore = GetPlayerTwoScore(goal, match);

            bool matchEnded = GetMatchEnded(goal);

            DateTime? startTime = GetStartTime(match);
            DateTime? endTime = GetEndTime(goal, match);

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"UPDATE single_league_matches 
                    SET player_one_score = @player_one_score, 
                    player_two_score = @player_two_score,
                    match_ended = @match_ended,
                    start_time = @start_time,
                    end_time = @end_time
                    WHERE id = @id",
                    new { 
                        player_one_score = playerOneScore,
                        player_two_score = playerTwoScore,
                        match_ended = matchEnded,
                        start_time = startTime,
                        end_time = endTime,
                        id = goal.MatchId
                    });
            }
        }

        private int GetPlayerOneScore(SingleLeagueGoalModel goal, SingleLeagueMatchModel match)
        {
            if (goal.ScoredByUserId == match.PlayerOne) {
                return goal.ScorerScore;
            }
            else {
                return goal.OpponentScore;
            }
        }

        private int GetPlayerTwoScore(SingleLeagueGoalModel goal, SingleLeagueMatchModel match)
        {
            if (goal.ScoredByUserId == match.PlayerOne) {
                return goal.OpponentScore;
            }
            else {
                return goal.ScorerScore;
            }
        }

        private bool GetMatchEnded(SingleLeagueGoalModel goal)
        {
            return goal.WinnerGoal == true;
        }

        private DateTime? GetStartTime(SingleLeagueMatchModel match)
        {
            if (match.StartTime == null)
            {
                return DateTime.Now;
            }
            else 
            {
                return match.StartTime;
            }
        }

        private DateTime? GetEndTime(SingleLeagueGoalModel goal, SingleLeagueMatchModel match)
        {
            if (goal.WinnerGoal == true)
            {
                return DateTime.Now;
            }
            else 
            {
                return match.EndTime;
            }
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
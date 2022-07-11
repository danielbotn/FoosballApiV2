using Dapper;
using FoosballApi.Dtos.DoubleGoals;
using FoosballApi.Models.Goals;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandDoubleGoalService
    {
        Task<IEnumerable<FreehandDoubleGoalsExtendedDto>> GetAllFreehandGoals(int matchId, int userId);
        Task<FreehandDoubleGoalModel> GetFreehandDoubleGoal(int goalId);
        Task<bool> CheckGoalPermission(int userId, int matchId, int goalId);
        FreehandDoubleGoalModel CreateDoubleFreehandGoal(int userId, FreehandDoubleGoalCreateDto freehandDoubleGoalCreateDto);
        void DeleteFreehandGoal(FreehandDoubleGoalModel goalItem);
        void UpdateFreehanDoubledGoal(FreehandDoubleGoalModel goalItem);
        bool SaveChanges();
    }

    public class FreehandDoubleGoalService : IFreehandDoubleGoalService
    {
        public string _connectionString { get; }

        public FreehandDoubleGoalService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<FreehandDoubleGoalPermission> GetFreehandDoubleGoalPermission(int goalId, int matchId)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT double_match_id as DoubleMatchId, scored_by_user_id as ScoredByUserId, 
                    player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA, 
                    player_one_team_b as PlayerOneTeamB, player_two_team_b as PlayerTwoTeamB
                    FROM freehand_double_goals fdg
                    JOIN freehand_double_matches fdm ON fdg.double_match_id = fdm.id
                    WHERE fdg.double_match_id = @double_match_id AND fdg.id = @goal_id";
                return await connection.QueryFirstOrDefaultAsync<FreehandDoubleGoalPermission>(sql, new { double_match_id = matchId, goal_id = goalId });
            }
        }

        public async Task<bool> CheckGoalPermission(int userId, int matchId, int goalId)
        {
            var query = await GetFreehandDoubleGoalPermission(goalId, matchId);

            if (query.DoubleMatchId == matchId &&
                (userId == query.PlayerOneTeamA || userId == query.PlayerOneTeamB
                || userId == query.PlayerTwoTeamA || userId == query.PlayerTwoTeamB))
                return true;

            return false;
        }

        public FreehandDoubleGoalModel CreateDoubleFreehandGoal(int userId, FreehandDoubleGoalCreateDto freehandDoubleGoalCreateDto)
        {
            throw new NotImplementedException();
        }

        public void DeleteFreehandGoal(FreehandDoubleGoalModel goalItem)
        {
            throw new NotImplementedException();
        }

        private async Task<List<FreehandDoubleGoalsJoinDto>> GetAllFreehandDoubleGoalsJoin(int matchId)
        {
            List<FreehandDoubleGoalsJoinDto> result = new();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<FreehandDoubleGoalsJoinDto>(
                    @"SELECT DISTINCT fdg.Id as Id, scored_by_user_id as ScoredByUserId, fdg.double_match_id as DoubleMatchId, 
                    fdg.scorer_team_score, fdg.opponent_team_score, fdg.winner_goal, fdg.time_of_goal as TimeOfGoal, 
                    u.first_name as FirstName, u.last_name as LastName, u.email as Email, u.photo_url as PhotoUrl
                    FROM freehand_double_goals fdg
                    JOIN freehand_double_matches fdm ON fdm.player_one_team_a = fdg.scored_by_user_id OR 
                    fdm.player_one_team_b = fdg.scored_by_user_id OR fdm.player_two_team_a = fdg.scored_by_user_id OR 
                    fdm.player_two_team_b = fdg.scored_by_user_id
                    JOIN users u on fdg.scored_by_user_id = u.id
                    WHERE fdg.double_match_id = @double_match_id
                    ORDER BY fdg.id desc",
                new { double_match_id = matchId });
                return goals.ToList();
            }
        }

        private FreehandDoubleMatchModel GetFreehandDoubleMatch(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = conn.QueryFirstOrDefault<FreehandDoubleMatchModel>(
                    @"SELECT id as Id, player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA, 
                    player_one_team_b as PlayerOneTeamB, player_two_team_b as PlayerTwoTeamB, organisation_id as OrganisationId, 
                    start_time as StartTime, end_time as EndTime, team_a_score as TeamAScore, team_b_score as TeamBScore, 
                    nickname_team_a as NickNameTeamA, nickname_team_b as NickNameTeamB, up_to as UpTo, game_finished as GameFinished, 
                    game_paused as GamePaused
                    FROM freehand_double_matches
                    WHERE id = @id",
                    new { id = matchId });
                return match;
            }
        }

        private string CalculateGoalTimeStopWatch(DateTime timeOfGoal, int matchId)
        {
            var match = GetFreehandDoubleMatch(matchId);
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

        public async Task<IEnumerable<FreehandDoubleGoalsExtendedDto>> GetAllFreehandGoals(int matchId, int userId)
        {
            List<FreehandDoubleGoalsExtendedDto> result = new List<FreehandDoubleGoalsExtendedDto>();
            var query = await GetAllFreehandDoubleGoalsJoin(matchId);

            foreach (var item in query)
            {
                FreehandDoubleGoalsExtendedDto fdg = new FreehandDoubleGoalsExtendedDto
                {
                    Id = item.Id,
                    ScoredByUserId = item.ScoredByUserId,
                    DoubleMatchId = item.DoubleMatchId,
                    ScorerTeamScore = item.ScorerTeamScore,
                    OpponentTeamScore = item.OpponentTeamScore,
                    WinnerGoal = item.WinnerGoal,
                    TimeOfGoal = item.TimeOfGoal,
                    GoalTimeStopWatch = CalculateGoalTimeStopWatch(item.TimeOfGoal, item.DoubleMatchId),
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Email = item.Email,
                    PhotoUrl = item.PhotoUrl
                };
                result.Add(fdg);
            }

            return result;
        }

        public async Task<FreehandDoubleGoalModel> GetFreehandDoubleGoal(int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<FreehandDoubleGoalModel>(
                    @"SELECT id as Id, time_of_goal as TimeOfGoal, double_match_id as DoubleMatchId,
                    scored_by_user_id as ScoredByUserId, scorer_team_score as ScorerTeamScore, 
                    opponent_team_score as OpponentTeamScore, winner_goal as WinnerGoal
                    FROM freehand_double_goals
                    WHERE id = @id",
                    new { id = goalId });
                return match;
            }
            throw new NotImplementedException();
        }

        public bool SaveChanges()
        {
            throw new NotImplementedException();
        }

        public void UpdateFreehanDoubledGoal(FreehandDoubleGoalModel goalItem)
        {
            throw new NotImplementedException();
        }
    }
}
using Dapper;
using FoosballApi.Dtos.DoubleGoals;
using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Models.Goals;
using FoosballApi.Models.Matches;
using Hangfire;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandDoubleGoalService
    {
        Task<IEnumerable<FreehandDoubleGoalsExtendedDto>> GetAllFreehandGoals(int matchId, int userId);
        Task<FreehandDoubleGoalModel> GetFreehandDoubleGoal(int goalId);
        Task<bool> CheckGoalPermission(int userId, int matchId, int goalId);
        Task<FreehandDoubleGoalModel> CreateDoubleFreehandGoal(int userId, FreehandDoubleGoalCreateDto freehandDoubleGoalCreateDto);
        Task DeleteFreehandGoal(FreehandDoubleGoalModel goalItem);
        void UpdateFreehanDoubledGoal(FreehandDoubleGoalModel goalItem);
    }

    public class FreehandDoubleGoalService : IFreehandDoubleGoalService
    {
        public string _connectionString { get; }
        private readonly ISlackService _slackService;
        private readonly IDiscordService _discordService;

        public FreehandDoubleGoalService(ISlackService slackService, IDiscordService discordService)
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif

            _slackService = slackService;
            _discordService = discordService;
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

        private async Task<FreehandDoubleMatchModel> GetFreehandDoubleMatchById(int id)
        {
            // þarf að athuga þetta
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    SELECT id as Id, player_one_team_a as PlayerOneTeamA,
                    player_two_team_a as PlayerTwoTeamA, player_one_team_b as PlayerOneTeamB,
                    player_two_team_b as PlayerTwoTeamB, organisation_id as OrganisationId, start_time as StartTime, end_time as EndtTime,
                    team_a_score as TeamAScore, team_b_score as TeamBScore, up_to as UpTo, game_finished as GameFinished, game_paused as GamePaused,
                    nickname_team_a as NicknameTeamA, nickname_team_b as NicknameTeamB
                    FROM freehand_double_matches
                    WHERE id = @id";
                var data = await connection.QueryFirstOrDefaultAsync<FreehandDoubleMatchModel>(sql, new { id = id });

                return data;
            }
        }

        // used for slack message
        private async Task<OrganisationModel> GetOrganisationById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var organisation = await conn.QueryFirstOrDefaultAsync<OrganisationModel>(
                @"SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_type as OrganisationType, organisation_code AS OrganisationCode,
                    slack_webhook_url as SlackWebhookUrl, discord_webhook_url AS DiscordWebhookUrl
                    FROM organisations
                    WHERE id = @id",
            new { id = id });
            return organisation;
        }

        private async Task<User> GetUserById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                @"SELECT u.id, u.email, u.first_name as FirstName, u.last_name as LastName, u.created_at, 
                    u.current_organisation_id as CurrentOrganisationId, u.photo_url as PhotoUrl , o.is_admin as IsAdmin,
                    u.refresh_token as RefreshToken, u.refresh_token_expiry_time as RefreshTokenExpiryTime,
                    o.is_deleted as IsDeleted
                    FROM Users u
                    LEFT JOIN organisation_list o ON o.user_id = u.id AND o.organisation_id = u.current_organisation_id
                    WHERE u.id = @id",
                new { id });
            return user;
        }

        private async Task<bool> IsSlackIntegrated(int userId)
        {
            bool result = false;
            User playerOne = await GetUserById(userId);
            if (playerOne != null && playerOne.CurrentOrganisationId != null)
            {
                OrganisationModel data = await GetOrganisationById(playerOne.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    result = true;
                }
            }

            return result;
        }

        private async Task<bool> IsDiscordIntegrated(int userId)
        {
            bool result = false;
            User playerOne = await GetUserById(userId);
            if (playerOne != null && playerOne.CurrentOrganisationId != null)
            {
                OrganisationModel data = await GetOrganisationById(playerOne.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.DiscordWebhookUrl))
                {
                    result = true;
                }
            }

            return result;
        }

        public async Task SendSlackMessageIfIntegrated(FreehandDoubleMatchModel match, int userId)
        {
            await Task.Delay(1);
            BackgroundJob.Enqueue(() => _slackService.SendSlackMessageForFreehandDoubleGame(match, userId));
        }

        public async Task SendDiscordMessageIfIntegrated(FreehandDoubleMatchModel match, int userId)
        {
            await Task.Delay(1);
            BackgroundJob.Enqueue(() => _discordService.SendDiscordMessageForFreehandDoubleGame(match, userId));
        }

        private async Task<bool> UpdateFreehandDoubleMatchScore(int userId, FreehandDoubleGoalCreateDto freehandGoalCreateDto)
        {
            bool result = false;
            int updateStatment;
            FreehandDoubleMatchModel fmm = await GetFreehandDoubleMatchById(freehandGoalCreateDto.DoubleMatchId);
            if (fmm.PlayerOneTeamA == freehandGoalCreateDto.ScoredByUserId || fmm.PlayerTwoTeamA == freehandGoalCreateDto.ScoredByUserId)
            {
                fmm.TeamAScore = freehandGoalCreateDto.ScorerTeamScore;
            }
            else
            {
                fmm.TeamBScore = freehandGoalCreateDto.ScorerTeamScore;
            }

            // Check if match is finished
            if (freehandGoalCreateDto.WinnerGoal == true)
            {
                fmm.EndTime = DateTime.Now;
                fmm.GameFinished = true;

                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                        UPDATE freehand_double_matches
                        SET team_a_score = @team_a_score, team_b_score = @team_b_score, 
                        end_time = @end_time, game_finished = @game_finished, game_paused = @game_paused
                        WHERE id = @id";
                updateStatment = await connection.ExecuteAsync(sql, new
                {
                    team_a_score = fmm.TeamAScore,
                    team_b_score = fmm.TeamBScore,
                    end_time = fmm.EndTime,
                    game_finished = fmm.GameFinished,
                    game_paused = false,
                    id = fmm.Id
                });

                if (await IsSlackIntegrated(userId))
                {
                    await SendSlackMessageIfIntegrated(fmm, userId);
                }

                if (await IsDiscordIntegrated(userId))
                {
                    await SendDiscordMessageIfIntegrated(fmm, userId);
                }
            }
            else
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var sql = @"
                        UPDATE freehand_double_matches
                        SET team_a_score = @team_a_score, team_b_score = @team_b_score
                        WHERE id = @id";
                updateStatment = await connection.ExecuteAsync(sql, new
                {
                    team_a_score = fmm.TeamAScore,
                    team_b_score = fmm.TeamBScore,
                    id = fmm.Id
                });
            }

            if (updateStatment > 0)
            {
                result = true;
            }

            return result;
        }

        public async Task<FreehandDoubleGoalModel> CreateDoubleFreehandGoal(int userId, FreehandDoubleGoalCreateDto freehandDoubleGoalCreateDto)
        {
            FreehandDoubleGoalModel fhg = new FreehandDoubleGoalModel();
            DateTime now = DateTime.Now;
            fhg.DoubleMatchId = freehandDoubleGoalCreateDto.DoubleMatchId;
            fhg.OpponentTeamScore = freehandDoubleGoalCreateDto.OpponentTeamScore;
            fhg.ScoredByUserId = freehandDoubleGoalCreateDto.ScoredByUserId;
            fhg.ScorerTeamScore = freehandDoubleGoalCreateDto.ScorerTeamScore;
            fhg.TimeOfGoal = now;
            fhg.WinnerGoal = freehandDoubleGoalCreateDto.WinnerGoal;
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var nGoal = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO freehand_double_goals (time_of_goal, double_match_id, scored_by_user_id, scorer_team_score, opponent_team_score, winner_goal)
                    VALUES (@time_of_goal, @double_match_id, @scored_by_user_id, @scorer_team_score, @opponent_team_score, @winner_goal)
                    RETURNING id",
                    new { 
                        time_of_goal = now, 
                        double_match_id = freehandDoubleGoalCreateDto.DoubleMatchId, 
                        scored_by_user_id = freehandDoubleGoalCreateDto.ScoredByUserId,
                        scorer_team_score = freehandDoubleGoalCreateDto.ScorerTeamScore,
                        opponent_team_score = freehandDoubleGoalCreateDto.OpponentTeamScore,
                        winner_goal = freehandDoubleGoalCreateDto.WinnerGoal,
                    });
               
                fhg.Id = nGoal;
            }

            bool updateSuccessfull = await UpdateFreehandDoubleMatchScore(userId, freehandDoubleGoalCreateDto);

            if (!updateSuccessfull)
                throw new Exception("Could not update freehand double match");

            return fhg;
        }

        private async Task<bool> SubtractDoubleFreehandMatchScore(FreehandDoubleGoalModel freehandDoubleGoalModel)
        {
            bool result = false;
            int updateStatement;
            var match = await GetFreehandDoubleMatchById(freehandDoubleGoalModel.DoubleMatchId);

            if (freehandDoubleGoalModel.ScoredByUserId == match.PlayerOneTeamA || freehandDoubleGoalModel.ScoredByUserId == match.PlayerTwoTeamA)
            {
                if (match.TeamAScore > 0)
                    match.TeamAScore -= 1;
            }
            else
            {
                if (match.TeamBScore > 0)
                    match.TeamBScore -= 1;
            }
           
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    UPDATE freehand_double_matches
                    SET team_a_score = @team_a_score, team_b_score = @team_b_score
                    WHERE id = @id";
                updateStatement = await connection.ExecuteAsync(sql, new {
                    team_a_score = match.TeamAScore,
                    team_b_score = match.TeamBScore,
                    id = match.Id
                });
            }
            
            if (updateStatement > 0)
                result = true;
            
            return result;
        }

        public async Task DeleteFreehandGoal(FreehandDoubleGoalModel goalItem)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var sql = @"
                    DELETE FROM freehand_double_goals
                    WHERE id = @id";
                connection.Execute(sql, new { id = goalItem.Id });
            }
            bool updateSuccessfull = await SubtractDoubleFreehandMatchScore(goalItem);

            if (!updateSuccessfull)
                throw new Exception("Could not delete freehand double goal");
        }

        private async Task<List<FreehandDoubleGoalsJoinDto>> GetAllFreehandDoubleGoalsJoin(int matchId)
        {
            List<FreehandDoubleGoalsJoinDto> result = new();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goals = await conn.QueryAsync<FreehandDoubleGoalsJoinDto>(
                    @"SELECT DISTINCT fdg.Id as Id, scored_by_user_id as ScoredByUserId, fdg.double_match_id as DoubleMatchId, 
                    fdg.scorer_team_score as ScorerTeamScore, fdg.opponent_team_score as OpponentTeamScore, fdg.winner_goal as WinnerGoal, 
                    fdg.time_of_goal as TimeOfGoal, u.first_name as FirstName, u.last_name as LastName, 
                    u.email as Email, u.photo_url as PhotoUrl
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
        }

        public void UpdateFreehanDoubledGoal(FreehandDoubleGoalModel goalItem)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE freehand_double_goals 
                    SET time_of_goal = @time_of_goal, 
                    double_match_id = @double_match_id, 
                    scored_by_user_id = @scored_by_user_id, 
                    scorer_team_score = @scorer_team_score, 
                    opponent_team_score = @opponent_team_score, 
                    winner_goal = @winner_goal
                    WHERE id = @id",
                    new { 
                        time_of_goal = goalItem.TimeOfGoal,
                        double_match_id = goalItem.DoubleMatchId,
                        scored_by_user_id = goalItem.ScoredByUserId,
                        scorer_team_score = goalItem.ScorerTeamScore,
                        opponent_team_score = goalItem.OpponentTeamScore,
                        winner_goal = goalItem.WinnerGoal,
                        id = goalItem.Id
                     });
            }
        }
    }
}
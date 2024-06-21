using Dapper;
using FoosballApi.Dtos.Goals;
using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Models.Goals;
using FoosballApi.Models.Matches;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IFreehandGoalService
    {
        Task<IEnumerable<FreehandGoalModelExtended>> GetFreehandGoalsByMatchId(int matchId, int userId);
        Task<bool> CheckGoalPermission(int userId, int matchId, int goalId);
        Task<FreehandGoalModelExtended> GetFreehandGoalById(int goalId);
        Task<FreehandGoalModel> GetFreehandGoalByIdFromDatabase(int goalId);
        Task<FreehandGoalModel> CreateFreehandGoal(int userId, FreehandGoalCreateDto freehandGoalCreateDto);
        Task DeleteFreehandGoal(FreehandGoalModel freehandGoalModel);
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

        public async Task<bool> CheckGoalPermission(int userId, int matchId, int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<FreehandGoalPermission>(
                    @"SELECT fg.match_id as MatchId, fg.scored_by_user_Id as ScoredByUserId, 
                    fm. player_one_Id as PlayerOneId, fm.player_two_id as PlayerTwoId
                    FROM freehand_goals fg
                    JOIN freehand_matches fm ON fg.match_id = fm.id
                    WHERE fg.match_id = @match_id AND fg.id = @goal_id",
                    new { match_id = matchId, goal_id = goalId });

                if (data.MatchId == matchId && (userId == data.PlayerOneId || userId == data.PlayerTwoId))
                {
                    return true;
                }
                return false;
            }
        }

        private async Task<FreehandGoalModel> GetFreehandGoalByIdDapper(int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var goal = await conn.QueryFirstOrDefaultAsync<FreehandGoalModel>(
                    @"SELECT id as Id, time_of_goal as TimeOfGoal, match_id as MatchId,
                    scored_by_user_id as ScoredByUserId, Oponent_id as OponentId,
                    scored_by_score as ScoredByScore, oponent_score as OponentScore,
                    winner_goal as WinnerGoal
                    FROM freehand_goals
                    WHERE id = @goal_id",
                    new { goal_id = goalId });
                return goal;
            }
        }

        public async Task<FreehandGoalModelExtended> GetFreehandGoalById(int goalId)
        {
            var data = await GetFreehandGoalByIdDapper(goalId);

            FreehandGoalModelExtended result = new FreehandGoalModelExtended
            {
                Id = data.Id,
                TimeOfGoal = data.TimeOfGoal,
                MatchId = data.MatchId,
                ScoredByUserId = data.ScoredByUserId,
                ScoredByUserFirstName = await GetFirstNameOfUser(data.ScoredByUserId),
                ScoredByUserLastName = await GetLastNameOfUser(data.ScoredByUserId),
                ScoredByUserPhotoUrl = await GetPhotoUrlOfUser(data.ScoredByUserId),
                OponentId = data.OponentId,
                OponentFirstName = await GetFirstNameOfUser(data.OponentId),
                OponentLastName = await GetLastNameOfUser(data.OponentId),
                OponentPhotoUrl = await GetPhotoUrlOfUser(data.OponentId),
                ScoredByScore = data.ScoredByScore,
                OponentScore = data.OponentScore,
                WinnerGoal = data.WinnerGoal
            };

            return result;
        }

        // used for slack message
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

        // curl -X POST -H 'Content-type: application/json' --data '{"text":"Hello, World!"}' https://hooks.slack.com/services/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
        public async Task SendSlackMessage(FreehandMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    _webhookUrl = data.SlackWebhookUrl;
                }
            }

            User playerOne = await GetUserById(match.PlayerOneId);
            User playerTwo = await GetUserById(match.PlayerTwoId);

            string winnerName;
            string loserName;
            int winnerScore;
            int loserScore;

            if (match.PlayerOneScore > match.PlayerTwoScore)
            {
                winnerName = $"{playerOne.FirstName} {playerOne.LastName}";
                loserName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                winnerScore = match.PlayerOneScore;
                loserScore = match.PlayerTwoScore;
            }
            else
            {
                winnerName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                loserName = $"{playerOne.FirstName} {playerOne.LastName}";
                winnerScore = match.PlayerTwoScore;
                loserScore = match.PlayerOneScore;
            }

            TimeSpan matchDuration = match.EndTime.HasValue ? match.EndTime.Value - match.StartTime : TimeSpan.Zero;

            string formattedDuration;
            if (matchDuration.TotalMinutes < 1)
            {
                formattedDuration = $"{matchDuration.Seconds} seconds";
            }
            else if (matchDuration.TotalHours < 1)
            {
                formattedDuration = $"{(int)matchDuration.TotalMinutes} minutes";
            }
            else
            {
                formattedDuration = $"{(int)matchDuration.TotalHours} hours and {(int)matchDuration.Minutes} minutes";
            }

            var message = new
            {
                text = $"Game Results:\n\n" +
                    $"Winner: {winnerName}\n" +
                    $"Loser: {loserName}\n" +
                    $"Final Score: {winnerScore} - {loserScore}\n" +
                    $"Match Duration: {formattedDuration}"
            };

            string bodyParam = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCall(bodyParam, _webhookUrl);
        }

        private async Task<OrganisationModel> GetOrganisationById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var organisation = await conn.QueryFirstOrDefaultAsync<OrganisationModel>(
                @"SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_type as OrganisationType, organisation_code AS OrganisationCode,
                    slack_webhook_url as SlackWebhookUrl
                    FROM organisations
                    WHERE id = @id",
            new { id = id });
            return organisation;
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

        private async void UpdateFreehandMatchScore(int userId, FreehandGoalCreateDto freehandGoalCreateDto)
        {
            var fmm = await GetFreehandMatchById(freehandGoalCreateDto.MatchId);
            if (fmm.PlayerOneId == freehandGoalCreateDto.ScoredByUserId)
            {
                fmm.PlayerOneScore = freehandGoalCreateDto.ScoredByScore;
            }
            else
            {
                fmm.PlayerTwoScore = freehandGoalCreateDto.ScoredByScore;
            }

            // Check if match is finished
            if (freehandGoalCreateDto.WinnerGoal == true)
            {
                fmm.EndTime = DateTime.Now;
                fmm.GameFinished = true;

                // Send slack message
                if (await IsSlackIntegrated(userId))
                {
                    await SendSlackMessage(fmm, userId);
                }
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"UPDATE freehand_matches
                    SET player_one_score = @player_one_score, player_two_score = @player_two_score,
                    end_time = @end_time, game_finished = @game_finished
                    WHERE id = @match_id",
                    new { player_one_score = fmm.PlayerOneScore, player_two_score = fmm.PlayerTwoScore,
                        end_time = fmm.EndTime, game_finished = fmm.GameFinished, match_id = fmm.Id });
            }
        }

        public async Task<FreehandGoalModel> CreateFreehandGoal(int userId, FreehandGoalCreateDto freehandGoalCreateDto)
        {
            FreehandGoalModel fhg = new FreehandGoalModel();
            DateTime now = DateTime.Now;
            fhg.MatchId = freehandGoalCreateDto.MatchId;
            fhg.OponentScore = freehandGoalCreateDto.OponentScore;
            fhg.ScoredByScore = freehandGoalCreateDto.ScoredByScore;
            fhg.ScoredByUserId = freehandGoalCreateDto.ScoredByUserId;
            fhg.OponentId = freehandGoalCreateDto.OponentId;
            fhg.TimeOfGoal = now;
            fhg.WinnerGoal = freehandGoalCreateDto.WinnerGoal;
           
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryFirstOrDefaultAsync<FreehandGoalModel>(
                    @"INSERT INTO freehand_goals (time_of_goal, match_id, scored_by_user_id, oponent_id, scored_by_score, oponent_score, winner_goal)
                    VALUES (@time_of_goal, @match_id, @scored_by_user_id, @oponent_id, @scored_by_score, @oponent_score, @winner_goal)
                    RETURNING id as Id
                    ",
                    new 
                    { 
                        time_of_goal = fhg.TimeOfGoal, 
                        match_id = fhg.MatchId,
                        scored_by_user_id = fhg.ScoredByUserId, 
                        oponent_id = fhg.OponentId, 
                        scored_by_score = fhg.ScoredByScore, 
                        oponent_score = fhg.OponentScore, 
                        winner_goal = fhg.WinnerGoal 
                    });
                    fhg.Id = data.Id;
                    UpdateFreehandMatchScore(userId, freehandGoalCreateDto);
                    return fhg;
            }
        }

        private async Task SubtractFreehandMatchScore(FreehandGoalModel freehandGoalModel)
        {
            var match = await GetFreehandMatchById(freehandGoalModel.MatchId);
            if (freehandGoalModel.ScoredByUserId == match.PlayerOneId)
            {
                if (match.PlayerOneScore > 0)
                    match.PlayerOneScore -= 1;
            }
            else
            {
                if (match.PlayerTwoScore > 0)
                    match.PlayerTwoScore -= 1;
            }
            
            // update match score
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"UPDATE freehand_matches
                    SET player_one_score = @player_one_score, player_two_score = @player_two_score
                    WHERE id = @match_id",
                    new { player_one_score = match.PlayerOneScore, player_two_score = match.PlayerTwoScore, match_id = match.Id });
            }
        }

        private async Task DeleteGoal(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"DELETE FROM freehand_goals
                    WHERE id = @id",
                    new { id = id });
            }
        }

        public async Task DeleteFreehandGoal(FreehandGoalModel freehandGoalModel)
        {
            if (freehandGoalModel == null)
            {
                throw new ArgumentNullException(nameof(freehandGoalModel));
            }
            await SubtractFreehandMatchScore(freehandGoalModel);
            await DeleteGoal(freehandGoalModel.Id);
        }

        public async Task<FreehandGoalModel> GetFreehandGoalByIdFromDatabase(int goalId)
        {
           var data = await GetFreehandGoalByIdDapper(goalId);
           return data;
        }
    }
}
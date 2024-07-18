using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FoosballApi.Dtos.DoubleLeagueGoals;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueGoals;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeaguePlayers;
using Hangfire;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeagueGoalService
    {
        Task<IEnumerable<DoubleLeagueGoalExtended>> GetAllDoubleLeagueGoalsByMatchId(int matchId);
        Task<DoubleLeagueGoalDapper> GetDoubleLeagueGoalById(int goalId);
        Task<bool> CheckPermissionByGoalId(int goalId, int userId);
        Task<DoubleLeagueGoalModel> CreateDoubleLeagueGoal(DoubleLeagueGoalCreateDto doubleLeagueGoalCreateDto);
        void DeleteDoubleLeagueGoal(int goalId);
    }

    public class DoubleLeagueGoalService : IDoubleLeagueGoalService
    {
        public string _connectionString { get; }
        private readonly ISlackService _slackService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;
        private readonly IDiscordService _discordService;
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        public DoubleLeagueGoalService(
            ISlackService slackService, 
            IDoubleLeaugeMatchService doubleLeagueMatchService, 
            IDiscordService discordService, 
            IUserService userService, 
            IOrganisationService organisationService)
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif

            _slackService = slackService;
            _doubleLeagueMatchService = doubleLeagueMatchService;
            _discordService = discordService;
            _userService = userService;
            _organisationService = organisationService;
        }

        private async Task<int> GetMatchIdFromDoubleLeagueGoals(int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT match_id as MatchId FROM double_league_goals WHERE id = @goalId",
                    new { goalId });
                return user;
            }
        }

        private async Task<DoubleLeagueMatchModel> GetDoubleLeagueMatchData(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<DoubleLeagueMatchModel>(
                    @"select id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, 
                    league_id as LeagueId, start_time as StartTime, end_time as EndTime,
                    team_one_score as TeamOneScore, team_two_score as TeamTwoScore,
                    match_started as MatchStarted, match_ended as MatchEnded, 
                    match_paused as MatchPaused
                    from double_league_matches
                    where id = @id",
                    new { id });
                return match;
            }
        }
        
        private List<DoubleLeaguePlayerModel> GetDoubleLeaguePlayersByTeamId(int teamId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var players = conn.Query<DoubleLeaguePlayerModel>(
                    @"SELECT id as Id, user_id as UserId, double_league_team_id as DoubleLeagueTeamId
                    FROM double_league_players WHERE double_league_team_id = @teamId",
                    new { teamId });
                return players.ToList();
            }
        }


        public async Task<bool> CheckPermissionByGoalId(int goalId, int userId)
        {
            bool result = false;
            List<int> teamIds = new List<int>();
            int matchId = await GetMatchIdFromDoubleLeagueGoals(goalId);
            var matchData = await GetDoubleLeagueMatchData(matchId);
            int leagueId = matchData.LeagueId;

            teamIds.Add(matchData.TeamOneId);
            teamIds.Add(matchData.TeamTwoId);

            foreach (var item in teamIds)
            {
                var doubleLeaguePlayerData = GetDoubleLeaguePlayersByTeamId(item);

                foreach (var element in doubleLeaguePlayerData)
                {
                    if (element.UserId == userId)
                    {
                        result = true;
                        break;
                    }

                }

            }

            return result;
        }

        private async Task UpdateDoubleLeagueMatch(NpgsqlConnection conn, DoubleLeagueGoalCreateDto doubleLeagueGoalCreateDto)
        {
           var doubleLeagueMatch = await GetDoubleLeagueMatchByIdAsync(doubleLeagueGoalCreateDto.MatchId);
           if (doubleLeagueGoalCreateDto.ScoredByTeamId == doubleLeagueMatch.TeamOneId)
           {
                await conn.ExecuteAsync(
                @"UPDATE double_league_matches 
                SET team_one_score = @team_one_score
                WHERE id = @id",
                new
                {
                    team_one_score = doubleLeagueGoalCreateDto.ScorerTeamScore,
                    id = doubleLeagueGoalCreateDto.MatchId
                });
           }
           else if (doubleLeagueGoalCreateDto.ScoredByTeamId == doubleLeagueMatch.TeamTwoId)
           {
                await conn.ExecuteAsync(
                @"UPDATE double_league_matches 
                SET team_two_score = @team_two_score
                WHERE id = @id",
                new
                {
                    team_two_score = doubleLeagueGoalCreateDto.ScorerTeamScore,
                    id = doubleLeagueGoalCreateDto.MatchId
                });
           }
        }

        private static async Task EndDoubleLeagueMatch(NpgsqlConnection conn, int matchId)
        {
            await conn.ExecuteAsync(
                @"UPDATE double_league_matches 
                SET end_time = @end_time, match_ended = @match_ended
                WHERE id = @id",
                new
                {
                    end_time = DateTime.Now,
                    match_ended = true,
                    id = matchId
                });
        }

        public async Task<DoubleLeagueGoalModel> CreateDoubleLeagueGoal(DoubleLeagueGoalCreateDto doubleLeagueGoalCreateDto)
        {
            DateTime now = DateTime.Now;
            DoubleLeagueGoalModel newGoal = new()
            {
                TimeOfGoal = now,
                MatchId = doubleLeagueGoalCreateDto.MatchId,
                ScoredByTeamId = doubleLeagueGoalCreateDto.ScoredByTeamId,
                OpponentTeamId = doubleLeagueGoalCreateDto.OpponentTeamId,
                ScorerTeamScore = doubleLeagueGoalCreateDto.ScorerTeamScore,
                OpponentTeamScore = doubleLeagueGoalCreateDto.OpponentTeamScore
            };

            if (doubleLeagueGoalCreateDto.WinnerGoal != null)
                newGoal.WinnerGoal = (bool)doubleLeagueGoalCreateDto.WinnerGoal;

            newGoal.UserScorerId = doubleLeagueGoalCreateDto.UserScorerId;

            // use dapper to create new goal
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var nGoal = await conn.ExecuteAsync(
                    @"INSERT INTO double_league_goals (time_of_goal, match_id, scored_by_team_id, opponent_team_id,
                    scorer_team_score, opponent_team_score, winner_goal, user_scorer_id)
                    VALUES (@time_of_goal, @match_id, @scored_by_team_id, @opponent_team_id, @scorer_team_score, @opponent_team_score,
                    @winner_goal, @user_scorer_id)
                    RETURNING id",
                    new { 
                        time_of_goal = newGoal.TimeOfGoal, 
                        match_id = newGoal.MatchId, 
                        scored_by_team_id = newGoal.ScoredByTeamId,
                        opponent_team_id = newGoal.OpponentTeamId,
                        scorer_team_score = newGoal.ScorerTeamScore,
                        opponent_team_score = newGoal.OpponentTeamScore,
                        winner_goal = newGoal.WinnerGoal,
                        user_scorer_id = newGoal.UserScorerId });
               
                newGoal.Id = nGoal;

                await UpdateDoubleLeagueMatch(conn, doubleLeagueGoalCreateDto);

                if (newGoal.WinnerGoal == true)
                {
                    await EndDoubleLeagueMatch(conn, doubleLeagueGoalCreateDto.MatchId);
                    
                    if (await IsSlackIntegrated(doubleLeagueGoalCreateDto.UserScorerId))
                    {

                        // send slack message
                        await Task.Delay(1);
                        var match = await _doubleLeagueMatchService.GetMatchById(doubleLeagueGoalCreateDto.MatchId);
                        BackgroundJob.Enqueue(() => _slackService.SendSlackMessageForDoubleLeague(match, doubleLeagueGoalCreateDto.UserScorerId));
                    }

                    if (await IsDiscordkIntegrated(doubleLeagueGoalCreateDto.UserScorerId))
                    {
                        // send discord message
                        await Task.Delay(1);
                        var match = await _doubleLeagueMatchService.GetMatchById(doubleLeagueGoalCreateDto.MatchId);
                        BackgroundJob.Enqueue(() => _discordService.SendSDiscordMessageForDoubleLeague(match, doubleLeagueGoalCreateDto.UserScorerId));
                    }
                }
            }

            return newGoal;
        }

        private async Task<bool> IsSlackIntegrated(int userId)
        {
            bool result = false;
            User playerOne = await _userService.GetUserById(userId);
            if (playerOne != null && playerOne.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(playerOne.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    result = true;
                }
            }

            return result;
        }

        private async Task<bool> IsDiscordkIntegrated(int userId)
        {
            bool result = false;
            User playerOne = await _userService.GetUserById(userId);
            if (playerOne != null && playerOne.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(playerOne.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.DiscordWebhookUrl))
                {
                    result = true;
                }
            }

            return result;
        }

        private async void UpdateDoubleLeagueMatch(DoubleLeagueMatchModel match)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(
                @"UPDATE double_league_matches 
                    SET team_one_score = @team_one_score, team_two_score = @team_two_score
                    WHERE id = @id",
                new
                {
                    team_one_score = match.TeamOneScore,
                    team_two_score = match.TeamTwoScore,
                    id = match.Id
                });
        }

        private async void DeleteDoubleLeagueGoalAsync(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"DELETE FROM double_league_goals WHERE id = @id",
                    new { id = id });
            }
        }

        public async void DeleteDoubleLeagueGoal(int goalId)
        {
            var goalToDelete = await GetDoubleLeagueGoalById(goalId);
            int scoredByTeamId = goalToDelete.ScoredByTeamId;

            var doubleLeagueMatch = await GetDoubleLeagueMatchByIdAsync(goalToDelete.MatchId);
            
            if (doubleLeagueMatch.TeamOneId == scoredByTeamId)
            {
                if (doubleLeagueMatch.TeamOneScore > 0)
                    doubleLeagueMatch.TeamOneScore -= 1;
            }

            if (doubleLeagueMatch.TeamTwoId == scoredByTeamId)
            {
                if (doubleLeagueMatch.TeamTwoScore > 0)
                    doubleLeagueMatch.TeamTwoScore -= 1;
            }

            UpdateDoubleLeagueMatch(doubleLeagueMatch);
            DeleteDoubleLeagueGoalAsync(goalId);
        }

        public async Task<List<DoubleLeagueGoalDapper>> GetDoubleLeagueGoals(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<DoubleLeagueGoalDapper>(
                    @"
                    select distinct dlg.id as Id, dlg.time_of_goal as TimeOfGoal, dlg.scored_by_team_id as ScoredByTeam, 
                    dlg.opponent_team_id as OpponentTeamId, dlg.scorer_team_score as ScorerTeamScore, 
                    dlg.opponent_team_score as OpponentTeamScore, dlg.winner_goal as WinnerGoal,
                    dlg.user_scorer_id as UserScorerId, dlp.double_league_team_id as DoubleLeagueTeamId,
                    u.first_name as ScorerFirstName, u.last_name as ScorerLastName, u.photo_url as ScorerPhotoUrl 
                    from double_league_goals dlg
                    join double_league_players dlp on dlp.double_league_team_id = dlg.scored_by_team_id 
                    join users u on u.id = dlg.user_scorer_id 
                    where dlg.match_id = @matchId
                    order by dlg.id
                    ",
                new { matchId });
                return users.ToList();
            }
        }

        public async Task<IEnumerable<DoubleLeagueGoalExtended>> GetAllDoubleLeagueGoalsByMatchId(int matchId)
        {
            var dapperReadData = await GetDoubleLeagueGoals(matchId);
            
            List<DoubleLeagueGoalExtended> result = new();
            
            foreach (var item in dapperReadData)
            {
                DoubleLeagueGoalExtended dlge = new DoubleLeagueGoalExtended{
                    Id = item.Id,
                    TimeOfGoal = item.TimeOfGoal,
                    ScoredByTeamId = item.ScoredByTeamId,
                    OpponentTeamId = item.OpponentTeamId,
                    ScorerTeamScore = item.ScorerTeamScore,
                    OpponentTeamScore = item.OpponentTeamScore,
                    WinnerGoal = item.WinnerGoal,
                    UserScorerId = item.UserScorerId,
                    ScorerFirstName = item.ScorerFirstName,
                    ScorerLastName = item.ScorerLastName,
                    ScorerPhotoUrl = item.ScorerPhotoUrl,
                    GoalTimeStopWatch = CalculateGoalTimeStopWatch(item.TimeOfGoal, matchId),
                };
                result.Add(dlge);
            }

            return result;
        }

        public async Task<DoubleLeagueGoalDapper> GetDoubleLeagueGoalById(int goalId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<DoubleLeagueGoalDapper>(
                    @"
                    select distinct dlg.id as Id, dlg.time_of_goal as TimeOfGoal, dlg.match_id as MatchId, dlg.scored_by_team_id as ScoredByTeamId, 
                    dlg.opponent_team_id as OpponentTeamId, dlg.scorer_team_score as ScorerTeamScore, 
                    dlg.opponent_team_score as OpponentTeamScore, dlg.winner_goal as WinnerGoal, dlg.user_scorer_id as UserScorerId, 
                    dlp.double_league_team_id as DoubleLeagueTeamId, u.first_name as ScorerFfirstName, 
                    u.last_name as ScorerLastName
                    from double_league_goals dlg
                    join double_league_players dlp on dlp.double_league_team_id = dlg.scored_by_team_id
                    join users u on u.id = dlg.user_scorer_id
                    where dlg.id = @goalId
                    order by dlg.id
                    ",
                new { goalId });
                return users.FirstOrDefault();
            }
            
        }

        private async Task<DoubleLeagueMatchModel> GetDoubleLeagueMatchByIdAsync(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var match = await conn.QueryFirstOrDefaultAsync<DoubleLeagueMatchModel>(
                    @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @id",
                    new { id });
                return match;
            }
        }

        private DoubleLeagueMatchModel GetDoubleLeagueMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = conn.QueryFirstOrDefault<DoubleLeagueMatchModel>(
                    @"SELECT id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, start_time as StartTime,
                    end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore, match_started as MatchStarted, 
                    match_ended as MatchEnded, match_paused as MatchPaused 
                    FROM double_league_matches WHERE id = @id",
                    new { id = matchId });
                return user;
            }
        }

        private string CalculateGoalTimeStopWatch(DateTime timeOfGoal, int matchId)
        {
            var match = GetDoubleLeagueMatchById(matchId);
            DateTime? matchStarted = match.StartTime;
            DateTime dateTimeValue = (DateTime)matchStarted; // Replace this with your DateTime object
            string matchStartedAsString = dateTimeValue.ToString("yyyy-MM-dd HH:mm");
            string timeOfGoalAsString = timeOfGoal.ToString("yyyy-MM-dd HH:mm");
            
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
    }
}
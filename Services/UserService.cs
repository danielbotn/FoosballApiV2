using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Enums;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Users;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsers(int currentOrganisationId);
        Task<User> GetUserById(int id);
        User GetUserByIdSync(int id);
        void UpdateUser(User user);
        void DeleteUser(User user);
        Task<UserStats> GetUserStats(int userId);
        IEnumerable<Match> GetLastTenMatchesByUserId(int userId);
    }

    public class UserService : IUserService
    {
        public string _connectionString { get; }

        public UserService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

       public async Task<List<User>> GetAllUsers(int currentOrganisationId)
       {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE current_organisation_id = @currentOrganisationId",
                new { currentOrganisationId });
                return users.ToList();
            }
        }

        public async Task<User> GetUserById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstOrDefaultAsync<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE id = @id",
                    new { id });
                return user;
            }
        }

        public User GetUserByIdSync(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = conn.QueryFirstOrDefault<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE id = @id",
                    new { id });
                return user;
            }
        }

        public void UpdateUser(User user)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE Users SET email = @email, first_name = @firstName, last_name = @lastName, 
                    current_organisation_id = @currentOrganisationId, photo_url = @photoUrl 
                    WHERE id = @id",
                    new { user.Email, user.FirstName, user.LastName, user.CurrentOrganisationId, user.PhotoUrl, user.Id });
            }
        }

        public void DeleteUser(User user)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM Users WHERE id = @id",
                    new { user.Id });
            }
        }

        private async Task<int> GetTotalSingleFreehandMatches(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM freehand_matches 
                    WHERE player_one_id = @userId OR player_two_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsPlayerOne(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM freehand_matches 
                    WHERE player_one_id = @userId AND player_one_score > player_two_score",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsPlayerTwo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM freehand_matches 
                    WHERE player_two_id = @userId AND player_two_score > player_one_score",
                    new { userId });
                return count;
            }
        }

        private async Task<(int, int)> GetSingleFreehandMatchesWonAndLost(int userId)
        {
            (int, int) result = (0, 0);
            int totalSingleFreehandMatches = await GetTotalSingleFreehandMatches(userId);
            int totalMatchesWonAsPlayerOne = await GetTotalMatchesWonAsPlayerOne(userId);
            int totalMatchesWonAsPlayerTwo = await GetTotalMatchesWonAsPlayerTwo(userId);
            int totalMatchesWon = totalMatchesWonAsPlayerOne + totalMatchesWonAsPlayerTwo;

            result.Item1 = totalMatchesWon;
            result.Item2 = totalSingleFreehandMatches - totalMatchesWon;

            return result;
        }

        private async Task<int> GetTotalDoubleFreehandMatches(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) 
                    FROM freehand_double_matches 
                    WHERE player_one_team_a = @userId OR player_one_team_b = @userId 
                    OR player_two_team_a = @userId OR player_two_team_b = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsTeamA(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM freehand_double_matches
                    WHERE (player_one_team_a = @userId OR 
                    player_two_team_a = @userId) AND team_a_score > team_b_score",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsTeamB(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM freehand_double_matches
                    WHERE (player_one_team_b = @userId OR 
                    player_two_team_b = @userId) AND team_b_score > team_a_score",
                    new { userId });
                return count;
            }

        }

        private async Task<(int, int)> GetDoubleFreehandMatchesWonAndLost(int userId)
        {
            (int, int) result = (0, 0);
            int totalDoubleFreehandMatches = await GetTotalDoubleFreehandMatches(userId);
            int totalMatchesWonAsTeamA = await GetTotalMatchesWonAsTeamA(userId);
            int totalMatchesWonAsTeamB = await GetTotalMatchesWonAsTeamB(userId);

            int totalMatchesWon = totalMatchesWonAsTeamA + totalMatchesWonAsTeamB;
            result.Item1 = totalMatchesWon;
            result.Item2 = totalDoubleFreehandMatches - totalMatchesWon;

            return result;
        }

        private async Task<int> GetTotalSingleLeagueMatches(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) FROM single_league_matches
                    WHERE player_one = @userId OR player_two = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsPlayerOneSingleLeague(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) 
                    FROM single_league_matches
                    WHERE player_one = @userId AND player_one_score > player_two_score",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsPlayerTwoSingleLeague(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(*) 
                    FROM single_league_matches
                    WHERE player_two = @userId AND player_two_score > player_one_score",
                    new { userId });
                return count;
            }
        }


        private async Task<(int, int)> GetSingleLeagueMatchesWonAndLost(int userId)
        {
            (int, int) result = (0, 0);
            int totalSingleLeagueMatches = await GetTotalSingleLeagueMatches(userId);
            int totalMatchesWonAsPlayerOne = await GetTotalMatchesWonAsPlayerOneSingleLeague(userId);
            int totalMatchesWonAsPlayerTwo = await GetTotalMatchesWonAsPlayerTwoSingleLeague(userId);
            int totalMatchesWon = totalMatchesWonAsPlayerOne + totalMatchesWonAsPlayerTwo;

            result.Item1 = totalMatchesWon;
            result.Item2 = totalSingleLeagueMatches - totalMatchesWon;

            return result;
        }

        private async Task<int> GetCountOfDoubleLeagueMatches(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(dlm.id) 
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_one_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetCountOfDoubleLeagueMatchesTwo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(dlm.id) 
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_two_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalDoubleLeagueMatches(int userId)
        {
            int doubleLeagueMatchesCount = await GetCountOfDoubleLeagueMatches(userId);
            int doubleLeagueMatchesCountTwo = await GetCountOfDoubleLeagueMatchesTwo(userId);

            return doubleLeagueMatchesCount + doubleLeagueMatchesCountTwo;
        }

        private async Task<int> GetTotalMatchesWonAsTeamOneDoubleLeague(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT count(dlm.id) FROM double_league_matches dlm 
                    JOIN double_league_players dlp on dlm.team_one_id = dlp.double_league_team_id 
                    WHERE dlp.user_id = @userId AND dlm.team_one_score > dlm.team_two_score
                    ",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalMatchesWonAsTeamTwoDoubleLeague(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"
                    select count(dlm.id)
                    from double_league_matches dlm
                    join double_league_players dlp on dlm.team_two_id = dlp.double_league_team_id
                    where dlp.user_id = @userId AND dlm.team_two_score > dlm.team_one_score
                    ",
                    new { userId });
                return count;
            }
        }

        private async Task<(int, int)> GetDoubleLeagueMatchesWonAndLost(int userId)
        {
            (int, int) result = (0, 0);
            int totalMatches = await GetTotalDoubleLeagueMatches(userId);
            int matchesWonAsTeamOne = await GetTotalMatchesWonAsTeamOneDoubleLeague(userId);
            int matchesWonAsTeamTwo = await GetTotalMatchesWonAsTeamTwoDoubleLeague(userId);
            int totalMatchesWon = matchesWonAsTeamOne + matchesWonAsTeamTwo;

            result.Item1 = totalMatchesWon;
            result.Item2 = totalMatches - totalMatchesWon;

            return result;
        }

        private async Task<int> GetTotalFreehandGoalsScored(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM freehand_goals
                    WHERE scored_by_user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalFreehandGoalsReceived(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM freehand_goals
                    WHERE oponent_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<(int, int)> GetFreehandGoalsScoredAndReceived(int userId)
        {
            (int, int) result = (0, 0);

            int totalGoalsScored = await GetTotalFreehandGoalsScored(userId);
            int totalGoalsReceived = await GetTotalFreehandGoalsReceived(userId);

            result.Item1 = totalGoalsScored;
            result.Item2 = totalGoalsReceived;

            return result;
        }

        private async Task<int> GetTotalFreehandDoubleGoalsScored(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM freehand_double_goals
                    WHERE scored_by_user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int?> GetTotalFreehandDoubleGoalsReceivedAsTeamA(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT SUM(team_b_score) FROM freehand_double_matches
                    WHERE player_one_team_a = @userId OR player_two_team_a = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int?> GetTotalFreehandDoubleGoalsReceivedAsTeamB(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT SUM(team_a_score) FROM freehand_double_matches
                    WHERE player_one_team_b = @userId OR player_two_team_b = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<(int, int?)> GetDoubleFreehandGoalsScoredAndReceived(int userId)
        {
            (int, int?) result = (0, 0);

            int totalGoalsScored = await GetTotalFreehandDoubleGoalsScored(userId);
            int? totalGoalsReceivedAsTeamA = await GetTotalFreehandDoubleGoalsReceivedAsTeamA(userId);
            int? totalGoalsReceivedAsTeamB = await GetTotalFreehandDoubleGoalsReceivedAsTeamB(userId);
            result.Item1 = totalGoalsScored;
            result.Item2 = totalGoalsReceivedAsTeamA + totalGoalsReceivedAsTeamB;
            return result;
        }

        private async Task<int> GetTotalSingleLeagueGoalsScored(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM single_league_goals
                    WHERE scored_by_user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalSingleLeagueGoalsReceived(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"SELECT COUNT(id) FROM single_league_goals
                    WHERE opponent_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<(int, int)> GetSingleLeagueGoalsScoredAndReceived(int userId)
        {
            (int, int) result = (0, 0);

            int totalGoalsScored = await GetTotalSingleLeagueGoalsScored(userId);
            int totalGoalsReceived = await GetTotalSingleLeagueGoalsReceived(userId);

            result.Item1 = totalGoalsScored;
            result.Item2 = totalGoalsReceived;

            return result;
        }

        private async Task<int> GetTotalDoubleLeagueGoalsScoredAsTeamOne(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"
                    SELECT SUM(dlm.team_one_score)
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_one_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalDoubleLeagueGoalsScoredAsTeamTwo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"
                    SELECT SUM(dlm.team_two_score)
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_two_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }
        
        private async Task<int> GetTotalDoubleLeagueGoalsReceivedAsTeamOne(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"
                    SELECT SUM(dlm.team_two_score)
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_one_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<int> GetTotalDoubleLeagueGoalsReceivedAsTeamTwo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var count = await conn.QueryFirstOrDefaultAsync<int>(
                    @"
                    SELECT SUM(dlm.team_one_score)
                    FROM double_league_matches dlm
                    JOIN double_league_players dlp on dlm.team_two_id = dlp.double_league_team_id
                    WHERE dlp.user_id = @userId",
                    new { userId });
                return count;
            }
        }

        private async Task<(int?, int?)> GetDoubleLeagueGoalsScoredAndReceived(int userId)
        {
            (int?, int?) result = (0, 0);

            int totalGoalsScoredAsTeamOne = await GetTotalDoubleLeagueGoalsScoredAsTeamOne(userId);
            int totalGoalsScoredAsTeamTwo = await GetTotalDoubleLeagueGoalsScoredAsTeamTwo(userId);
            int totalGoalsReceivedAsTeamOne = await GetTotalDoubleLeagueGoalsReceivedAsTeamOne(userId);
            int totalGoalsReceivedAsTeamTwo = await GetTotalDoubleLeagueGoalsReceivedAsTeamTwo(userId);

            int? totalGoalsAsTeamOne = totalGoalsScoredAsTeamOne;
            int? totalGoalsAsTeamTwo = totalGoalsScoredAsTeamTwo;
            int? totalReceivedGoalsAsTeamOne = totalGoalsReceivedAsTeamOne;
            int? totalReceivedGoalsAsTeamTwo = totalGoalsReceivedAsTeamTwo;

            result.Item1 = totalGoalsAsTeamOne + totalGoalsAsTeamTwo;
            result.Item2 = totalReceivedGoalsAsTeamOne + totalReceivedGoalsAsTeamTwo;
            return result;
        }

        private async Task<int> GetTotalMatchesByPlayer(int userId)
        {
            int result =
                await GetTotalDoubleLeagueMatches(userId)
                + await GetTotalSingleLeagueMatches(userId)
                + await GetTotalDoubleFreehandMatches(userId)
                + await GetTotalSingleFreehandMatches(userId);

            return result;
        }

        public async Task<UserStats> GetUserStats(int userId)
        {
            (int, int) freehandMatches = await GetSingleFreehandMatchesWonAndLost(userId);
            (int, int) doubleFreehandMatches = await GetDoubleFreehandMatchesWonAndLost(userId);
            (int, int) singleLeagueMatches = await GetSingleLeagueMatchesWonAndLost(userId);
            (int, int) doubleLeagueMatches = await GetDoubleLeagueMatchesWonAndLost(userId);

            (int, int) freeHandGoals = await GetFreehandGoalsScoredAndReceived(userId);
            (int, int?) doubleFreehandGoals = await GetDoubleFreehandGoalsScoredAndReceived(userId);
            (int, int) singleLeagueGoals = await GetSingleLeagueGoalsScoredAndReceived(userId);
            (int?, int?) doubleLeagueGoals = await GetDoubleLeagueGoalsScoredAndReceived(userId);

            UserStats userStats = new UserStats
            {
                UserId = userId,
                TotalMatches = await GetTotalMatchesByPlayer(userId),
                TotalFreehandMatches = await GetTotalSingleFreehandMatches(userId),
                TotalDoubleFreehandMatches = await GetTotalDoubleFreehandMatches(userId),
                TotalSingleLeagueMatches = await GetTotalSingleLeagueMatches(userId),
                TotalDoubleLeagueMatches = await GetTotalDoubleLeagueMatches(userId),
                TotalMatchesWon = freehandMatches.Item1 + doubleFreehandMatches.Item1 + singleLeagueMatches.Item1 + doubleLeagueMatches.Item1,
                TotalMatchesLost = freehandMatches.Item2 + doubleFreehandMatches.Item2 + singleLeagueMatches.Item2 + doubleLeagueMatches.Item2,
                TotalGoalsScored = freeHandGoals.Item1 + doubleFreehandGoals.Item1 + singleLeagueGoals.Item1 + (int)doubleLeagueGoals.Item1,
                TotalGoalsReceived = freeHandGoals.Item2 + (int)doubleFreehandGoals.Item2 + singleLeagueGoals.Item2 + (int)doubleLeagueGoals.Item2
            };

            return userStats;
        }

        private List<FreehandMatchModel> GetLastTenFreehandMatchesLimit(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = conn.Query<FreehandMatchModel>(
                    @"
                    SELECT fm.id, fm.player_one_id as PlayerOneId, fm.player_two_id as PlayerTwoId, 
                    fm.player_one_score as PlayerOneScore, fm.player_two_score as PlayerTwoScore, fm.end_time as EndTime
                    FROM freehand_matches fm
                    WHERE (fm.player_one_id = @userId OR fm.player_two_id = @userId) AND fm.end_time IS NOT NULL
                    ORDER BY fm.end_time DESC
                    LIMIT 10",
                    new { userId });
                return matches.ToList();
            }
        }

        private string GetOpponentOneFirstName(int userId, FreehandMatchModel freehandMatch)
        {
            string result = "";
            if (freehandMatch.PlayerOneId == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = freehandMatch.PlayerTwoId;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                   result = opponentOneFirstName.FirstName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = freehandMatch.PlayerOneId;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            return result;
        }

        private string GetOpponentOneLastName(int userId, FreehandMatchModel freehandMatch)
        {
            string result = "";
            if (freehandMatch.PlayerOneId == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = freehandMatch.PlayerTwoId;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = freehandMatch.PlayerOneId;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private IEnumerable<Match> GetLastTenFreehandMatches(int userId)
        {
            List<Match> result = new List<Match>();
            var freehandMatches = GetLastTenFreehandMatchesLimit(userId);

            foreach (var item in freehandMatches)
            {
                Match match = new Match
                {
                    TypeOfMatch = ETypeOfMatch.FreehandMatch,
                    TypeOfMatchName = ETypeOfMatch.FreehandMatch.ToString(),
                    UserId = userId,
                    TeamMateId = null,
                    MatchId = item.Id,
                    OpponentId = item.PlayerOneId == userId ? item.PlayerTwoId : item.PlayerOneId,
                    OpponentTwoId = null,
                    OpponentOneFirstName = GetOpponentOneFirstName(userId, item),
                    OpponentOneLastName = GetOpponentOneLastName(userId, item),
                    OpponentTwoFirstName = null,
                    OpponentTwoLastName = null,
                    UserScore = item.PlayerOneId == userId ? item.PlayerOneScore : item.PlayerTwoScore,
                    OpponentUserOrTeamScore = item.PlayerOneId == userId ? item.PlayerTwoScore : item.PlayerOneScore,
                    DateOfGame = (DateTime)item.EndTime
                };
                result.Add(match);
            }
            return result;
        }

        List<FreehandDoubleMatchModel> GetLastTenDoubleFreehandMatchesLimit(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = conn.Query<FreehandDoubleMatchModel>(
                    @"
                    SELECT fdm.id, fdm.player_one_team_a as PlayerOneTeamA, fdm.player_two_team_a as PlayerTwoTeamA, 
                    fdm.player_one_team_b as PlayerOneTeamB, fdm.player_two_team_b as PlayerTwoTeamB, 
                    fdm.team_a_score as TeamAScore, fdm.team_b_score as TeamBScore, fdm.end_time as EndTime
                    FROM freehand_double_matches fdm
                    WHERE player_one_team_a = @userId OR player_two_team_a = @userId OR 
                    player_one_team_b = @userId or player_two_team_b = @userId AND end_time != null
                    ORDER BY fdm.end_time DESC
                    LIMIT 10
                    ",
                    new { userId });
                return matches.ToList();
            }
        }

        private string GetFreehandDoubleOpponentOneFirstName(FreehandDoubleMatchModel item, string teamAorTeamB)
        {
            string result = null;
            if (teamAorTeamB == "teamA")
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOneTeamA;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else if (teamAorTeamB == "teamB")
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOneTeamB;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            return result;
        }

        private string GetFreehandDoubleOpponentOneLastName(FreehandDoubleMatchModel item, string teamAorTeamB)
        {
            string result = null;
            if (teamAorTeamB == "teamA")
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOneTeamA;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (teamAorTeamB == "teamB")
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOneTeamB;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private string GetFreehandDoubleOpponentTwoFirstName(FreehandDoubleMatchModel item, string teamAorTeamB)
        {
            string result = null;
            if (teamAorTeamB == "teamA" && item.PlayerTwoTeamA != null)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = (int)item.PlayerTwoTeamA;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (teamAorTeamB == "teamB" && item.PlayerTwoTeamB != null)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = (int)item.PlayerTwoTeamB;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private string GetFreehandDoubleOpponentTwoLastName(FreehandDoubleMatchModel item, string teamAorTeamB)
        {
            string result = null;
            if (teamAorTeamB == "teamA" && item.PlayerTwoTeamA != null)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = (int)item.PlayerTwoTeamA;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (teamAorTeamB == "teamB" && item.PlayerTwoTeamB != null)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = (int)item.PlayerTwoTeamB;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private User GetOpponentTwoData(int userId, FreehandDoubleMatchModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int playerTwoTeamB = (int)item.PlayerTwoTeamB;
                var opponentTwo = conn.QueryFirst<User>(
                    @"
                    SELECT id as Id, email as Email, first_name as FirstName, last_name as LastName, 
                    created_at as Created_at, current_organisation_id as CurrentOrganisationId, 
                    photo_url as PhotoUrl
                    FROM users
                    WHERE id = @playerTwoTeamB",
                    new { playerTwoTeamB });
                return opponentTwo;
            }
        }

        private string GetTeamMateFirstName(int userId, int? teamMateId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateFirstName = conn.QueryFirst<User>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @teamMateId",
                    new { teamMateId });
                return teamMateFirstName.FirstName;
            }
        }

        private string GetTeamMateLastName(int userId, int? teamMateId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateLastName = conn.QueryFirst<User>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @teamMateId",
                    new { teamMateId });
                return teamMateLastName.LastName;
            }
        }

        private IEnumerable<Match> GetLastTenFreehandDoubleMatches(int userId)
        {
            List<Match> result = new List<Match>();

            var lastTenDoubleFreehandMatches = GetLastTenDoubleFreehandMatchesLimit(userId);

            foreach (var item in lastTenDoubleFreehandMatches)
            {
                int theUserScore, theOpponentScore;
                string opponentOneFirstName, opponentOneLastName, opponentTwoFirstName, opponentTwoLastName;

                if (item.PlayerOneTeamA != userId && item.PlayerTwoTeamA != userId)
                {
                    opponentOneFirstName = GetFreehandDoubleOpponentOneFirstName(item, "teamA");
                    opponentOneLastName = GetFreehandDoubleOpponentOneLastName(item, "teamA");
                    opponentTwoFirstName = GetFreehandDoubleOpponentTwoFirstName(item, "teamA");
                    opponentTwoLastName = GetFreehandDoubleOpponentTwoLastName(item, "teamA");
                }
                else
                {
                    opponentOneFirstName = GetFreehandDoubleOpponentOneFirstName(item, "teamB");
                    opponentOneLastName = GetFreehandDoubleOpponentOneLastName(item, "teamB");

                    if (item.PlayerTwoTeamB != null)
                    {
                        var opponentTwoData = GetOpponentTwoData(userId, item);
                        if (opponentTwoData != null)
                        {
                            opponentTwoFirstName = opponentTwoData.FirstName;
                            opponentTwoLastName = opponentTwoData.LastName;
                        }
                        else
                        {
                            opponentTwoFirstName = null;
                            opponentTwoLastName = null;
                        }
                    }
                    else
                    {
                        opponentTwoFirstName = null;
                        opponentTwoLastName = null;
                    }
                }

                if (item.PlayerOneTeamA == userId || item.PlayerTwoTeamA == userId)
                {
                    theUserScore = (int)item.TeamAScore;
                    theOpponentScore = (int)item.TeamBScore;
                }
                else
                {
                    theUserScore = (int)item.TeamBScore;
                    theOpponentScore = (int)item.TeamAScore;
                }
                int? teamMateId = item.PlayerOneTeamA != userId && item.PlayerTwoTeamA != userId ? item.PlayerOneTeamB != userId ? item.PlayerTwoTeamB : item.PlayerOneTeamB : item.PlayerOneTeamA != userId ? item.PlayerTwoTeamA : item.PlayerOneTeamA;
                Match match = new Match
                {
                    TypeOfMatch = ETypeOfMatch.DoubleFreehandMatch,
                    TypeOfMatchName = ETypeOfMatch.DoubleFreehandMatch.ToString(),
                    MatchId = item.Id,
                    UserId = userId,
                    OpponentId = item.PlayerOneTeamA != userId && item.PlayerTwoTeamA != userId ? item.PlayerOneTeamB : item.PlayerOneTeamA,
                    OpponentTwoId = item.PlayerOneTeamA != userId && item.PlayerTwoTeamA != userId ? item.PlayerTwoTeamB : item.PlayerTwoTeamA,
                    TeamMateId = teamMateId,
                    TeamMateFirstName = teamMateId != null ? GetTeamMateFirstName(userId, teamMateId) : null,
                    TeamMateLastName = teamMateId != null ? GetTeamMateLastName(userId, teamMateId) : null,
                    OpponentOneFirstName = opponentOneFirstName,
                    OpponentOneLastName = opponentOneLastName,
                    OpponentTwoFirstName = opponentTwoFirstName,
                    OpponentTwoLastName = opponentTwoLastName,
                    UserScore = theUserScore,
                    OpponentUserOrTeamScore = theOpponentScore,
                    DateOfGame = (DateTime)item.EndTime,
                };
                result.Add(match);
            }
            return result;
        }

        private List<SingleLeagueMatchModel> GetLastTenSingleLeagueMatchesLimit(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var lastTenSingleLeagueMatches = conn.Query<SingleLeagueMatchModel>(
                    @"
                    select id as Id, player_one as PlayerOne, player_two as PlayerTwo, league_id as LeagueId, start_time as StartTime, 
                    end_time as EndTime, player_one_score as PlayerOneScore, player_two_score as PlayerTwoScore, 
                    match_started as MatchStarted, match_ended as MatchEnded, match_paused as MatchPause
                    FROM single_league_matches
                    WHERE (player_one = @userId OR player_two = @userId) AND match_ended = true
                    ORDER BY end_time DESC
                    LIMIT 10",
                    new { userId });
                return lastTenSingleLeagueMatches.ToList();
            }
        }

        private string GetSingleLeagueOpponentOneFirstName(int userId, SingleLeagueMatchModel item)
        {
            string result = "";
            if (item.PlayerOne == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerTwo;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOne;
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            return result;
        }

        private string GetSingleLeagueOpponentOneLastName(int userId, SingleLeagueMatchModel item)
        {
            string result = "";
            if (item.PlayerOne == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerTwo;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int opponentId = item.PlayerOne;
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private IEnumerable<Match> GetLastTenSingleLeagueMatches(int userId)
        {
            List<Match> result = new List<Match>();

            var lastTenSingleLeagueMatches = GetLastTenSingleLeagueMatchesLimit(userId);

            foreach (var item in lastTenSingleLeagueMatches)
            {
                int playerOne, playerTwo, playerOneScore, playerTwoScore;

                playerOne = item.PlayerOne;
                playerTwo = item.PlayerTwo;
                playerOneScore = (int)item.PlayerOneScore;
                playerTwoScore = (int)item.PlayerTwoScore;
                Match match = new Match
                {
                    TypeOfMatch = ETypeOfMatch.FreehandMatch,
                    TypeOfMatchName = ETypeOfMatch.FreehandMatch.ToString(),
                    UserId = userId,
                    TeamMateId = null,
                    MatchId = item.Id,
                    OpponentId = item.PlayerOne == userId ? item.PlayerTwo : item.PlayerOne,
                    OpponentTwoId = null,
                    OpponentOneFirstName = GetSingleLeagueOpponentOneFirstName(userId, item),
                    OpponentOneLastName = GetSingleLeagueOpponentOneLastName(userId, item),
                    OpponentTwoFirstName = null,
                    OpponentTwoLastName = null,
                    UserScore = playerOne == userId ? playerOneScore : playerTwoScore,
                    OpponentUserOrTeamScore = playerOne == userId ? playerTwoScore : playerOneScore,
                    DateOfGame = (DateTime)item.EndTime
                };
                result.Add(match);
            }
            return result;
        }

        private List<int> GetTeamIdsData(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamIds = conn.Query<int>(
                    @"
                    SELECT double_league_team_id 
                    FROM double_league_players
                    WHERE user_id = @userId",
                    new { userId });
                return teamIds.ToList();
            }
        }

        private List<DoubleLeagueMatchModel> GetDoubleLeagueMatchesTmp(int userId, int item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var tmp = conn.Query<DoubleLeagueMatchModel>(
                    @"
                    select id as Id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId, 
                    start_time as StartTime, end_time as EndTime, team_one_score as TeamOneScore, 
                    team_two_score as TeamTwoScore, match_started as MatchStarted, match_ended as MatchEnded, 
                    match_paused as MatchPaused
                    FROM double_league_matches
                    WHERE team_one_id = @item OR team_two_id = @item",
                    new { item });
                return tmp.ToList();
            }
        }

        private int GetUserIdDoubleLeagueMatches(int userId, DoubleLeagueMatchModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int temaOneId = item.TeamOneId;
                var userIds = conn.Query<int>(
                    @"
                    SELECT user_id
                    FROM double_league_players
                    WHERE double_league_team_id = @temaOneId",
                    new { temaOneId });
                return userIds.ToList().FirstOrDefault();
            }
        }

        private List<int> GetLastTenDoubleLeagueMatchesOpponentData(int userId, DoubleLeagueMatchModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int temaTwoId = item.TeamTwoId;
                var userIds = conn.Query<int>(
                    @"
                    SELECT user_id
                    FROM double_league_players
                    WHERE double_league_team_id = @temaTwoId
                    ORDER BY id",
                    new { temaTwoId });
                return userIds.ToList();
            }
        }

        private int GetLastTenMatchesUserId(int userId, DoubleLeagueMatchModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int temaTwoId = item.TeamTwoId;
                var userIds = conn.Query<int>(
                    @"
                    SELECT user_id
                    FROM double_league_players
                    WHERE double_league_team_id = @temaTwoId
                    ORDER BY id",
                    new { temaTwoId });
                return userIds.ToList().FirstOrDefault();
            }
        }

        private List<int> GetLastTenDoubleLeagueMatchesOpponentDataElse(int userId, DoubleLeagueMatchModel item)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int temaOneId = item.TeamOneId;
                var userIds = conn.Query<int>(
                    @"
                    SELECT user_id 
                    FROM double_league_players
                    WHERE double_league_team_id = @temaOneId",
                    new { temaOneId });
                return userIds.ToList();
            }
        }

        private string GetOpponentOneFirstNameDoubleLeagueMatches(int userId, int opponentId)
        {
            string result = "";
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentOneFirstName = conn.QueryFirst<User>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentOneFirstName.FirstName;
            }
            
            return result;
        }

        private string GetOpponentOneLastNameDoubleLeagueMatches(int userId, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentOneLastName = conn.QueryFirst<User>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentOneLastName.LastName;
            }
            return result;
        }

        private string GetOpponentTwoFirstNameDoubleLeagueMatches(int userId, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentTwoFirstName = conn.QueryFirst<User>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentTwoFirstName.FirstName;
            }
            return result;
        }
        
        private string GetOpponentTwoLastNameDoubleLeagueMatches(int userId, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentTwoLastName = conn.QueryFirst<User>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentTwoLastName.LastName;
            }
            return result;
        }

        private IEnumerable<Match> GetLastTenDoubleLeagueMatches(int userId)
        {
            List<DoubleLeagueMatchModel> doubleLeagueMatches = new();
            List<int> teamIds = new();
            List<Match> result = new List<Match>();

            // First find all team ids of user
            var teamIdsData = GetTeamIdsData(userId);

            foreach (var item in teamIdsData)
            {
                teamIds.Add(item);

                var tmp = GetDoubleLeagueMatchesTmp(userId, item);
                foreach (var element in tmp)
                {
                    doubleLeagueMatches.Add(element);
                }
            }

            foreach (var item in doubleLeagueMatches)
            {
                int opponentId;
                int opponentTwoId;
                int teamMateId;
                int userScore;
                int opponentScore;
                if (teamIds.Contains(item.TeamOneId))
                {
                    var uId = GetUserIdDoubleLeagueMatches(userId, item);
                    teamMateId = uId;
                    var opponentData = GetLastTenDoubleLeagueMatchesOpponentData(userId, item);

                    opponentId = opponentData.First();
                    opponentTwoId = opponentData.Last();
                    userScore = (int)item.TeamOneScore;
                    opponentScore = (int)item.TeamTwoScore;
                }
                else
                {
                    var uId = GetLastTenMatchesUserId(userId, item);
                    var opponentData = GetLastTenDoubleLeagueMatchesOpponentDataElse(userId, item);
                    teamMateId = uId;

                    opponentId = opponentData.First();
                    opponentTwoId = opponentData.Last();
                    userScore = (int)item.TeamTwoScore;
                    opponentScore = (int)item.TeamOneScore;
                }

                Match match = new Match
                {
                    TypeOfMatch = ETypeOfMatch.DoubleLeagueMatch,
                    TypeOfMatchName = ETypeOfMatch.DoubleLeagueMatch.ToString(),
                    UserId = userId,
                    TeamMateId = teamMateId,
                    MatchId = item.Id,
                    OpponentId = opponentId,
                    OpponentTwoId = null,
                    OpponentOneFirstName = GetOpponentOneFirstNameDoubleLeagueMatches(userId, opponentId),
                    OpponentOneLastName = GetOpponentOneLastNameDoubleLeagueMatches(userId, opponentId),
                    OpponentTwoFirstName = GetOpponentTwoFirstNameDoubleLeagueMatches(userId, opponentTwoId),
                    OpponentTwoLastName = GetOpponentTwoLastNameDoubleLeagueMatches(userId, opponentTwoId),
                    UserScore = userScore,
                    OpponentUserOrTeamScore = opponentScore,
                    DateOfGame = (DateTime)item.EndTime
                };
                result.Add(match);
            }

            return result;
        }

        private IEnumerable<Match> FilterLastTen(IEnumerable<Match> lastTen)
        {
            return lastTen.OrderByDescending(x => x.DateOfGame).Take(10);
        }

        public IEnumerable<Match> GetLastTenMatchesByUserId(int userId)
        {
            List<Match> result = new List<Match>();

            var lastTenFreehandMatches = GetLastTenFreehandMatches(userId);
            var lastTenFreehandDoubleMatches = GetLastTenFreehandDoubleMatches(userId);
            var lastTenSingleLeagueMatches = GetLastTenSingleLeagueMatches(userId);
            var lastTenDoubleLeagueMatches = GetLastTenDoubleLeagueMatches(userId);

            foreach (var fm in lastTenFreehandMatches)
            {
                result.Add(fm);
            }

            foreach (var fdm in lastTenFreehandDoubleMatches)
            {
                result.Add(fdm);
            }

            foreach (var slm in lastTenSingleLeagueMatches)
            {
                result.Add(slm);
            }

            foreach (var dlm in lastTenDoubleLeagueMatches)
            {
                result.Add(dlm);
            }

            return FilterLastTen(result);
        }

    }
}
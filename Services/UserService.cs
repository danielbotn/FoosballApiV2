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
            int selectOne = await GetCountOfDoubleLeagueMatches(userId);

            var selectTwo = await GetCountOfDoubleLeagueMatchesTwo(userId);

            return selectOne + selectTwo;
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

            var matchesWonAsTeamTwo = await GetTotalMatchesWonAsTeamTwoDoubleLeague(userId);

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

        // int totalGoalsReceived = _context.FreehandGoals.Where(x => x.OponentId == userId).Select(x => x.Id).Count();
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

    }
}
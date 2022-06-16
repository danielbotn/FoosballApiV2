using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Enums;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeaguePlayers;
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
        IEnumerable<Match> GetPagnatedHistory(int userId, int pageNumber, int pageSize);
        IEnumerable<Match> OrderMatchesByDescending(IEnumerable<Match> lastTen);
        User GetUserByEmail(string email);
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

        private List<FreehandMatchModel> PagnateFreehandMatches(int userId, int pageNumber, int pageSize)
        {
            List<FreehandMatchModel> result = new();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var freehandMatches = conn.Query<FreehandMatchModel>(
                    @"
                    SELECT id, player_one_id as PlayerOneId, player_two_id as PlayerTwoId, player_one_score as PlayerOneScore, player_two_score as PlayerTwoScore, end_time as EndTime, game_finished as GameFinished
                    FROM freehand_matches
                    WHERE (player_one_id = @userId OR player_two_id = @userId) AND game_finished = true
                    ORDER BY id DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                    new { userId, offset = (pageNumber - 1) * pageSize, pageSize });
                result = freehandMatches.ToList();
            }
            return result;
        }

        private string GetPagnatedFreehandOpponentOneFirstName(int userId, FreehandMatchModel match)
        {
            string result = "";
            if (match.PlayerOneId == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneId });
                    result = opponentOneFirstName.FirstName;
                }
            }
            return result;
        }

        private string GetPagnatedFreehandOpponentOneLastName(int userId, FreehandMatchModel match)
        {
            string result = "";
            if (match.PlayerOneId == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoId });
                    result = opponentOneLastName.LastName;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneId });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private string GetPagnatedFreehandOpponentOnePhotoUrl(int userId, FreehandMatchModel match)
        {
            string result = "";
            if (match.PlayerOneId == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoId });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            else
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneId });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            return result;
        }

        private List<Match> GetPagnatedFreehandMatches(int userId, int pageNumber, int pageSize)
        {
            List<Match> result = new List<Match>();

            var freehandMatches = PagnateFreehandMatches(userId, pageNumber, pageSize);

            foreach (var match in freehandMatches)
            {
                Match userLastTenItem = new Match();
                userLastTenItem.TypeOfMatch = ETypeOfMatch.FreehandMatch;
                userLastTenItem.TypeOfMatchName = ETypeOfMatch.FreehandMatch.ToString();
                userLastTenItem.UserId = userId;
                userLastTenItem.TeamMateId = null;
                userLastTenItem.MatchId = match.Id;
                userLastTenItem.OpponentId = match.PlayerOneId == userId ? match.PlayerTwoId : match.PlayerOneId;
                userLastTenItem.OpponentTwoId = null;
                userLastTenItem.OpponentOneFirstName = GetPagnatedFreehandOpponentOneFirstName(userId, match);
                userLastTenItem.OpponentOneLastName = GetPagnatedFreehandOpponentOneLastName(userId, match);
                userLastTenItem.OpponentOnePhotoUrl = GetPagnatedFreehandOpponentOnePhotoUrl(userId, match);
                userLastTenItem.OpponentTwoFirstName = null;
                userLastTenItem.OpponentTwoLastName = null;
                userLastTenItem.UserScore = match.PlayerOneId == userId ? match.PlayerOneScore : match.PlayerTwoScore;
                userLastTenItem.OpponentUserOrTeamScore = match.PlayerOneId == userId ? match.PlayerTwoScore : match.PlayerOneScore;
                userLastTenItem.DateOfGame = (DateTime)match.EndTime;

                result.Add(userLastTenItem);
            }

            return result;
        }

        private List<FreehandDoubleMatchModel> PagnateFreehandDoubleMatches(int userId, int pageNumber, int pageSize)
        {
            List<FreehandDoubleMatchModel> result = new List<FreehandDoubleMatchModel>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var freehandDoubleMatches = conn.Query<FreehandDoubleMatchModel>(
                    @"
                    SELECT id, player_one_team_a as PlayerOneTeamA, player_two_team_a as PlayerTwoTeamA, player_one_team_b as PlayerOneTeamB,
                    player_two_team_b as PlayerTwoTeamB, organisation_id as OrganisationId, start_time as StartTime, end_time as EndTime, team_a_score as TeamAScore, team_b_score as TeamBScore,
                    nickname_team_a as NickNameTeamA, nickname_team_b as NicknameTeamB, up_to as UpTo, game_finished as GameFinished, game_paused as GamePaused
                    FROM freehand_double_matches
                    WHERE (player_one_team_a = @userId OR player_two_team_a = @userId) AND game_finished = true
                    ORDER BY id DESC
                    OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY",
                    new { userId = userId, offset = (pageNumber - 1) * pageSize, pageSize = pageSize });
                result = freehandDoubleMatches.ToList();
            }
            return result;
        }

        private string GetTeamMateFirstNameUserLastTen(int userId, FreehandDoubleMatchModel match)
        {
            string result = "";
            if (match.PlayerOneTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamA });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else if (match.PlayerOneTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamB });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else if (match.PlayerTwoTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamA });
                    result = opponentOneFirstName.FirstName;
                }
            }
            else if (match.PlayerTwoTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneFirstName = conn.QueryFirst<User>(
                        @"
                        SELECT first_name as FirstName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamB });
                    result = opponentOneFirstName.FirstName;
                }
            }
            return result;
        }

        private string GetTeamMateLastNameUserLastTen(int userId, FreehandDoubleMatchModel match)
        {
            string result = "";
            if (match.PlayerOneTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamA });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (match.PlayerOneTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamB });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (match.PlayerTwoTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamA });
                    result = opponentOneLastName.LastName;
                }
            }
            else if (match.PlayerTwoTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOneLastName = conn.QueryFirst<User>(
                        @"
                        SELECT last_name as LastName
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamB });
                    result = opponentOneLastName.LastName;
                }
            }
            return result;
        }

        private string GetTeamMatePhotoUrlUserLastTen(int userId, FreehandDoubleMatchModel match)
        {
            string result = "";
            if (match.PlayerOneTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamA });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            else if (match.PlayerOneTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerTwoTeamB });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            else if (match.PlayerTwoTeamA == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamA });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            else if (match.PlayerTwoTeamB == userId)
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    var opponentOnePhotoUrl = conn.QueryFirst<User>(
                        @"
                        SELECT photo_url as PhotoUrl
                        FROM users
                        WHERE id = @opponentId",
                        new { opponentId = match.PlayerOneTeamB });
                    result = opponentOnePhotoUrl.PhotoUrl;
                }
            }
            return result;
        }

        private string GetOpponentOneFirstNameUserLastTen(int userId, int opponentId)
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

        private string GetOpponentOneLastNameUserLastTen(int userId, int opponentId)
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

        private string GetOpponentOnePhotUrlUserLastTen(int userId, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentOnePhotoUrl = conn.QueryFirst<User>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentOnePhotoUrl.PhotoUrl;
            }
            return result;
        }

        private string GetOpponentTwoFirstNameUserLastTen(int userId, int opponentId)
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

        private string GetOpponentTwoLastNameUserLastTen(int userId, int opponentId)
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

        private string GetOpponentTwoPhotoUrlUserLastTen(int userId, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentTwoPhotoUrl = conn.QueryFirst<User>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentTwoPhotoUrl.PhotoUrl;
            }
            return result;
        }

        private List<Match> GetPagnatedFreehandDoubleMatches(int userId, int pageNumber, int pageSize)
        {
            List<Match> result = new List<Match>();

            var freehandDoubleMatches = PagnateFreehandDoubleMatches(userId, pageNumber, pageSize);

            foreach (var match in freehandDoubleMatches)
            {
                Match userLastTenItem = new Match();
                int opponentId = match.PlayerOneTeamA != userId || match.PlayerTwoTeamA != userId ? match.PlayerOneTeamB : match.PlayerOneTeamA;
                int? oponentTwoId = match.PlayerOneTeamA != userId || match.PlayerTwoTeamA != userId ? match.PlayerTwoTeamB : match.PlayerTwoTeamA;

                userLastTenItem.TypeOfMatch = ETypeOfMatch.DoubleFreehandMatch;
                userLastTenItem.TypeOfMatchName = ETypeOfMatch.DoubleFreehandMatch.ToString();
                userLastTenItem.UserId = userId;
                userLastTenItem.TeamMateId = userId == match.PlayerOneTeamA ? match.PlayerTwoTeamA : userId == match.PlayerOneTeamB ? match.PlayerTwoTeamB :
                                            userId == match.PlayerTwoTeamA ? match.PlayerOneTeamA : userId == match.PlayerTwoTeamB ? match.PlayerOneTeamB : null;
                userLastTenItem.TeamMateFirstName = GetTeamMateFirstNameUserLastTen(userId, match) != "" ? GetTeamMateFirstNameUserLastTen(userId, match) : null;
                userLastTenItem.TeamMateLastName = GetTeamMateLastNameUserLastTen(userId, match) != "" ? GetTeamMateLastNameUserLastTen(userId, match) : null;
                userLastTenItem.TeamMatePhotoUrl = GetTeamMatePhotoUrlUserLastTen(userId, match) != "" ? GetTeamMatePhotoUrlUserLastTen(userId, match) : null;
                userLastTenItem.MatchId = match.Id;
                userLastTenItem.OpponentId = opponentId;
                userLastTenItem.OpponentTwoId = oponentTwoId;

                userLastTenItem.OpponentOneFirstName = GetOpponentOneFirstNameUserLastTen(userId, opponentId);
                userLastTenItem.OpponentOneLastName = GetOpponentOneLastNameUserLastTen(userId, opponentId);
                userLastTenItem.OpponentOnePhotoUrl = GetOpponentOnePhotUrlUserLastTen(userId, opponentId);

                userLastTenItem.OpponentTwoFirstName = GetOpponentTwoFirstNameUserLastTen(userId, (int)oponentTwoId);
                userLastTenItem.OpponentTwoLastName = GetOpponentTwoLastNameUserLastTen(userId, (int)oponentTwoId);
                userLastTenItem.OpponentTwoPhotoUrl = GetOpponentTwoPhotoUrlUserLastTen(userId, (int)oponentTwoId);
                userLastTenItem.UserScore = (int)match.PlayerOneTeamA == userId || (int)match.PlayerTwoTeamA == userId ? (int)match.TeamAScore : (int)match.TeamBScore;
                userLastTenItem.OpponentUserOrTeamScore = (int)match.PlayerOneTeamA != userId && (int)match.PlayerTwoTeamA != userId ? (int)match.TeamAScore : (int)match.TeamBScore;
                userLastTenItem.DateOfGame = (DateTime)match.EndTime;

                result.Add(userLastTenItem);
            }

            return result;
        }

        private List<SingleLeagueMatchModel> PagnateSingleLeagueMatches(int userId, int pageNumber, int pageSize)
        {
            List<SingleLeagueMatchModel> result = new List<SingleLeagueMatchModel>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var singleLeagueMatches = conn.Query<SingleLeagueMatchModel>(
                    @"
                    SELECT *
                    FROM single_league_matches
                    WHERE (player_one = @userId OR player_two = @userId) AND match_ended != null AND match_ended != false
                    ORDER BY id DESC
                    LIMIT @pageSize
                    OFFSET @pageNumber",
                    new { userId, pageNumber, pageSize });
                result = singleLeagueMatches.ToList();
            }
            return result;
        }

        private string GetOpponentOneFirstNameSingleLeaguePagnation(int userId, SingleLeagueMatchModel match, int opponentId)
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

        private string GetOpponetOneLastNameSingleLeaguePagnation(int userId, SingleLeagueMatchModel match, int opponentId)
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

        private string GetOpponetOnePhotoUrlSingleLeaguePagnation(int userId, SingleLeagueMatchModel match, int opponentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentOnePhotoUrl = conn.QueryFirst<User>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @opponentId",
                    new { opponentId });
                result = opponentOnePhotoUrl.PhotoUrl;
            }
            return result;
        }

        private List<Match> GetPagnatedSingleLeagueMatches(int userId, int pageNumber, int pageSize)
        {
            List<Match> userLastTen = new List<Match>();

            var singleLeagueMatches = PagnateSingleLeagueMatches(userId, pageNumber, pageSize);

            foreach (var match in singleLeagueMatches)
            {
                int oponentId = match.PlayerOne == userId ? match.PlayerTwo : match.PlayerOne;
                Match userLastTenItem = new Match();
                userLastTenItem.TypeOfMatch = ETypeOfMatch.SingleLeagueMatch;
                userLastTenItem.TypeOfMatchName = ETypeOfMatch.SingleLeagueMatch.ToString();
                userLastTenItem.UserId = userId;
                userLastTenItem.TeamMateId = null;
                userLastTenItem.TeamMateFirstName = null;
                userLastTenItem.TeamMateLastName = null;
                userLastTenItem.MatchId = match.Id;
                userLastTenItem.OpponentId = oponentId;
                userLastTenItem.OpponentTwoId = null;
                userLastTenItem.OpponentOneFirstName = GetOpponentOneFirstNameSingleLeaguePagnation(userId, match, oponentId);
                userLastTenItem.OpponentOneLastName = GetOpponetOneLastNameSingleLeaguePagnation(userId, match, oponentId);
                userLastTenItem.OpponentOnePhotoUrl = GetOpponetOnePhotoUrlSingleLeaguePagnation(userId, match, oponentId);
                userLastTenItem.OpponentTwoFirstName = null;
                userLastTenItem.OpponentTwoLastName = null;
                userLastTenItem.UserScore = userId == match.PlayerOne ? (int)match.PlayerOneScore : (int)match.PlayerTwoScore;
                userLastTenItem.OpponentUserOrTeamScore = userId == match.PlayerOne ? (int)match.PlayerTwoScore : (int)match.PlayerOneScore;
                userLastTenItem.DateOfGame = (DateTime)match.EndTime;
                userLastTenItem.LeagueId = match.LeagueId;
                userLastTen.Add(userLastTenItem);
            }

            return userLastTen;
        }

        private List<int> GetTeamIdsDataDoubleLeaguePagnation(int userId, int pageNumber, int pageSize)
        {
            List<int> teamIdsData = new List<int>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamIds = conn.Query<int>(
                    @"
                    SELECT double_league_team_id as TeamId
                    FROM double_league_players
                    WHERE user_id = @userId
                    ORDER BY id DESC
                    LIMIT @pageSize
                    OFFSET @pageNumber",
                    new { userId, pageNumber, pageSize });
                teamIdsData = teamIds.ToList();
            }
            return teamIdsData;
        }

        private List<DoubleLeagueMatchModel> GetDoubleLeagueMatchesForPagnation(int item)
        {
            List<DoubleLeagueMatchModel> result = new List<DoubleLeagueMatchModel>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var doubleLeagueMatches = conn.Query<DoubleLeagueMatchModel>(
                    @"
                    SELECT id, team_one_id as TeamOneId, team_two_id as TeamTwoId, league_id as LeagueId,
                    start_time as StartTime, end_time as EndTime, team_one_score as TeamOneScore, team_two_score as TeamTwoScore,
                    match_started as MatchStarted, match_ended as MatchEnded, match_paused as MatchPaused
                    FROM double_league_matches
                    WHERE (team_one_id = @item OR team_two_id = @item) && end_time != null
                    ORDER BY id DESC");
                result = doubleLeagueMatches.ToList();
            }
            return result;
        }

        private int GetTeamMateIdDoubleLeagueMatchesPagnation(int userId, DoubleLeagueMatchModel item)
        {
            int result = 0;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateId = conn.QueryFirst<int>(
                    @"
                    SELECT user_id as UserId
                    FROM double_league_players
                    WHERE double_league_team_id = @item AND user_id != @userId",
                    new { userId, item.TeamOneId });
                result = teamMateId;
            }
            return result;
        }

        private int GetTeamMateITeamTwoIddDoubleLeagueMatchesPagnation(int userId, DoubleLeagueMatchModel item)
        {
            int result = 0;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateId = conn.QueryFirst<int>(
                    @"
                    SELECT user_id as UserId
                    FROM double_league_players
                    WHERE double_league_team_id = @item AND user_id != @userId",
                    new { userId, item.TeamTwoId });
                result = teamMateId;
            }
            return result;
        }

        private List<DoubleLeaguePlayerModel> GetOppoentDateDoubleLeagueMatchesHelper(int userId, DoubleLeagueMatchModel match)
        {
            List<DoubleLeaguePlayerModel> opponentData = new List<DoubleLeaguePlayerModel>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentDataQuery = conn.Query<DoubleLeaguePlayerModel>(
                    @"
                    SELECT id, user_id as UserId
                    FROM double_league_players
                    WHERE double_league_team_id = @item",
                    new { item = match.TeamTwoId });
                opponentData = opponentDataQuery.ToList();
            }
            return opponentData;
        }

        private List<DoubleLeaguePlayerModel> GetOppoentDataTeamOneIdDoubleLeagueMatchesHelper(int userId, DoubleLeagueMatchModel match)
        {
            List<DoubleLeaguePlayerModel> opponentData = new List<DoubleLeaguePlayerModel>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentDataQuery = conn.Query<DoubleLeaguePlayerModel>(
                    @"
                    SELECT id, user_id as UserId
                    FROM double_league_players
                    WHERE double_league_team_id = @item",
                    new { item = match.TeamOneId });
                opponentData = opponentDataQuery.ToList();
            }
            return opponentData;
        }

        private string GetTeamMateFirstNameDoubleLeaugeMatches(int userId, int teamMateId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateFirstName = conn.QueryFirst<string>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @teamMateId",
                    new { teamMateId });
                result = teamMateFirstName;
            }
            return result;
        }

        private string GetTeamMateLastNameDoubleLeaugeMatches(int userId, int teamMateId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMateLastName = conn.QueryFirst<string>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @teamMateId",
                    new { teamMateId });
                result = teamMateLastName;
            }
            return result;
        }

        private string GetTeamMatePhotUrlDoubleLeaugeMatches(int userId, int teamMateId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var teamMatePhotoUrl = conn.QueryFirst<string>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @teamMateId",
                    new { teamMateId });
                result = teamMatePhotoUrl;
            }
            return result;
        }

        private string GetOpponentFirstNameDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentFirstName = conn.QueryFirst<string>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentFirstName;
            }
            return result;
        }

        private string GetOpponentLastNameDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentLastName = conn.QueryFirst<string>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentLastName;
            }
            return result;
        }

        private string GetOpponentOnePhotoUrlDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentPhotoUrl = conn.QueryFirst<string>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentPhotoUrl;
            }
            return result;
        }

        private string GetOpponentTwoFirstNameDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentTwoFirstName = conn.QueryFirst<string>(
                    @"
                    SELECT first_name as FirstName
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentTwoFirstName;
            }
            return result;
        }

        private string GetOpponentTwoLastNameDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentTwoLastName = conn.QueryFirst<string>(
                    @"
                    SELECT last_name as LastName
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentTwoLastName;
            }
            return result;
        }

        private string GetOpponentTwoPhotoUrlDoubleLeaugeMatches(int userId, int oppoentId)
        {
            string result = "";
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var opponentPhotoUrl = conn.QueryFirst<string>(
                    @"
                    SELECT photo_url as PhotoUrl
                    FROM users
                    WHERE id = @oppoentId",
                    new { oppoentId });
                result = opponentPhotoUrl;
            }
            return result;
        }

        private List<Match> GenerateDoubleLeagueMatches(List<DoubleLeagueMatchModel> doubleLeagueMatches, List<int> teamIds, int userId)
        {
            List<Match> result = new List<Match>();;
            foreach (var item in doubleLeagueMatches)
            {
                int opponentId;
                int opponentTwoId;
                int teamMateId;
                int userScore;
                int opponentScore;
                if (teamIds.Contains(item.TeamOneId))
                {
                    teamMateId =  GetTeamMateIdDoubleLeagueMatchesPagnation(userId, item);
                    var opponentData = GetOppoentDateDoubleLeagueMatchesHelper(userId, item);

                    opponentId = opponentData.First().Id;
                    opponentTwoId = opponentData.Last().Id;
                    userScore = (int)item.TeamOneScore;
                    opponentScore = (int)item.TeamTwoScore;
                }
                else
                {
                    teamMateId = GetTeamMateITeamTwoIddDoubleLeagueMatchesPagnation(userId, item);
                        
                    var opponentData = GetOppoentDataTeamOneIdDoubleLeagueMatchesHelper(userId, item);

                    opponentId = opponentData.First().Id;
                    opponentTwoId = opponentData.LastOrDefault().Id;
                    userScore = (int)item.TeamTwoScore;
                    opponentScore = (int)item.TeamOneScore;
                }

                Match match = new Match
                {
                    TypeOfMatch = ETypeOfMatch.DoubleLeagueMatch,
                    TypeOfMatchName = ETypeOfMatch.DoubleLeagueMatch.ToString(),
                    UserId = userId,
                    TeamMateId = teamMateId,
                    TeamMateFirstName = GetTeamMateFirstNameDoubleLeaugeMatches(userId, teamMateId),
                    TeamMateLastName = GetTeamMateLastNameDoubleLeaugeMatches(userId, teamMateId),
                    TeamMatePhotoUrl = GetTeamMatePhotUrlDoubleLeaugeMatches(userId, teamMateId),
                    MatchId = item.Id,
                    OpponentId = opponentId,
                    OpponentTwoId = null,
                    OpponentOneFirstName = GetOpponentFirstNameDoubleLeaugeMatches(userId, opponentId),
                    OpponentOneLastName = GetOpponentLastNameDoubleLeaugeMatches(userId, opponentId),
                    OpponentOnePhotoUrl = GetOpponentOnePhotoUrlDoubleLeaugeMatches(userId, opponentId),
                    OpponentTwoFirstName = GetOpponentTwoFirstNameDoubleLeaugeMatches(userId, opponentTwoId),
                    OpponentTwoLastName = GetOpponentTwoLastNameDoubleLeaugeMatches(userId, opponentTwoId),
                    OpponentTwoPhotoUrl = GetOpponentTwoPhotoUrlDoubleLeaugeMatches(userId, opponentTwoId),
                    UserScore = userScore,
                    OpponentUserOrTeamScore = opponentScore,
                    DateOfGame = (DateTime)item.EndTime,
                    LeagueId = item.LeagueId
                };
                result.Add(match);
            }

            return result;
        }

        private List<Match> GetPagnatedDoubleLeagueMatches(int userId, int pageNumber, int pageSize)
        {
            List<Match> result = new List<Match>();
            List<DoubleLeagueMatchModel> doubleLeagueMatches = new();
            List<int> teamIds = new();

            List<int> teamIdsData = GetTeamIdsDataDoubleLeaguePagnation(userId, pageNumber, pageSize);

            foreach (var item in teamIdsData)
            {
                teamIds.Add(item);

                var dlm = GetDoubleLeagueMatchesForPagnation(item);
                foreach (var element in dlm)
                {
                    doubleLeagueMatches.Add(element);
                }
            }

            return GenerateDoubleLeagueMatches(doubleLeagueMatches, teamIds, userId); ;
        }

        public IEnumerable<Match> OrderMatchesByDescending(IEnumerable<Match> lastTen)
        {
            return lastTen.OrderByDescending(x => x.DateOfGame);
        }

        public IEnumerable<Match> GetPagnatedHistory(int userId, int pageNumber, int pageSize)
        {
            List<Match> result = new List<Match>();

            List<Match> freehandMatches = GetPagnatedFreehandMatches(userId, pageNumber, pageSize);
            List<Match> freehandDoubleMatches = GetPagnatedFreehandDoubleMatches(userId, pageNumber, pageSize);
            List<Match> singleLeagueMatches = GetPagnatedSingleLeagueMatches(userId, pageNumber, pageSize);
            List<Match> doubleLeagueMatches = GetPagnatedDoubleLeagueMatches(userId, pageNumber, pageSize);

            foreach (var fm in freehandMatches)
            {
                result.Add(fm);
            }

            foreach (var fdm in freehandDoubleMatches)
            {
                result.Add(fdm);
            }

            foreach (var slm in singleLeagueMatches)
            {
                result.Add(slm);
            }

            foreach (var dlm in doubleLeagueMatches)
            {
                result.Add(dlm);
            }

            return result;
        }

        public User GetUserByEmail(string email)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = conn.QueryFirstOrDefault<User>(
                    @"
                    SELECT id, email as Email, first_name as FirstName, last_name as LastName, created_at as Created_at, current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl
                    FROM users
                    WHERE email = @email",
                    new { email });
                return user;
            }
        }
    }
}
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Leagues;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Other;
using Npgsql;

namespace FoosballApi.Services
{
    public interface ISingleLeagueMatchService
    {
        Task<bool> CheckLeaguePermission(int leagueId, int userId);
        Task<IEnumerable<SingleLeagueStandingsQuery>> GetSigleLeagueStandings(int leagueId);
    }

    public class SingleLeagueMatchService : ISingleLeagueMatchService
    {

        private string _connectionString { get; }
        public SingleLeagueMatchService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task<bool> CheckLeaguePermission(int leagueId, int userId)
        {
            bool result = false;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var players = await conn.QueryAsync<LeaguePlayersModel>(
                    @"SELECT id as Id, user_id as UserId, created_at as CreatedAt, league_id as LeagueId
                    FROM league_players
                    WHERE league_id = @league_id AND user_id = @user_id",
                new { league_id = leagueId, user_id = userId });
                
                var data = players.FirstOrDefault();

                if (data.UserId == userId && data.LeagueId == leagueId)
                    result = true;
            }

            return result;
        }

        private async Task<List<SingleLeagueStandingsAllPlayersQuery>> GetPlayers(int leagueId)
        {
            List<SingleLeagueStandingsAllPlayersQuery> result = new();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var players = await conn.QueryAsync<SingleLeagueStandingsAllPlayersQuery>(
                    @"SELECT id as Id, user_id as UserId
                    FROM league_players
                    WHERE league_id = @league_id AND user_id = @user_id",
                new { league_id = leagueId });
                
                return players.ToList();
            }
        }

        private async Task<List<int>> GetAllUsersOfLeague(int leagueId)
        {
            List<int> userIds = new();
            var allPlayersInLeague = await GetPlayers(leagueId);

            foreach (SingleLeagueStandingsAllPlayersQuery element in allPlayersInLeague)
            {
                userIds.Add(element.UserId);
            }
            return userIds;
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesWonAsPlayerOne(int userId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_one = @player_one AND match_ended = true AND player_one_score > player_two_score",
                new { player_one = userId });
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesWonAsPlayerTwo(int userId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_two = @player_two AND match_ended = true AND player_two_score > player_one_score",
                new { player_two = userId });
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesLostAsPlayerOne(int userId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_one = @player_one AND match_ended = true AND player_one_score < player_two_score",
                new { player_one = userId });
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesLostAsPlayerTwo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_two = @player_two AND match_ended = true AND player_two_score < player_one_score",
                new { player_two = userId });
                
                return matches.ToList();
            }
        }

        private async Task<User> GetUserInfo(int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstAsync<User>(
                    @"SELECT id as Id, email as Email, first_name as FirstName,
                    last_name as LastName
                    FROM users
                    WHERE id = @id",
                new { id = userId });
                
                return user;
            }
        }

        private async Task<int> GetTotalGoalsScored(int userId, int leagueId)
        {
            int result;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryAsync<User>(
                    @"SELECT slg.id as Id
                    FROM single_league_goals slg
                    JOIN single_league_matches slm ON slg.match_id = slm.id
                    WHERE slg.scored_by_user_id = @user_id AND slm.league_id = @league_id",
                new { user_id = userId, league_id = leagueId });
                
                result = user.ToList().Count();
            }

            return result;
        }

        private async Task<int> GetTotalGoalsRecieved(int userId, int leagueId)
        {
            int result;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryAsync<User>(
                    @"SELECT slg.id as Id
                    FROM single_league_goals slg
                    JOIN single_league_matches slm ON slg.match_id = slm.id
                    WHERE slg.opponent_id = @user_id AND slm.league_id = @league_id",
                new { user_id = userId, league_id = leagueId });
                
                result = user.ToList().Count();
            }

            return result;
        }

        private List<SingleLeagueStandingsQuery> ReturnSortedLeague(List<SingleLeagueStandingsQuery> singleLeagueStandings)
        {
            return singleLeagueStandings.OrderByDescending(x => x.Points).ToList();
        }

        private List<SingleLeagueStandingsQuery> AddPositionInLeagueToList(List<SingleLeagueStandingsQuery> standings)
        {
            List<SingleLeagueStandingsQuery> result = standings;
            foreach (var item in result.Select((value, i) => new { i, value }))
            {
                item.value.PositionInLeague = item.i + 1;
            }
            return result;
        }

        public async Task<IEnumerable<SingleLeagueStandingsQuery>> GetSigleLeagueStandings(int leagueId)
        {
            List<SingleLeagueStandingsQuery> standings = new();
            const int Points = 3;
            const int Zero = 0;
            List<int> userIds = await GetAllUsersOfLeague(leagueId);

            foreach (int userId in userIds)
            {
                var matchesWonAsPlayerOne = await GetMatchesWonAsPlayerOne(userId);
                var matchesWonAsPlayerTwo = await GetMatchesWonAsPlayerTwo(userId);

                var matchesLostAsPlayerOne = await GetMatchesLostAsPlayerOne(userId);
                var matchesLostAsPlayerTwo = await GetMatchesLostAsPlayerTwo(userId);

                User userInfo = await GetUserInfo(userId);

                int totalMatchesWon = matchesWonAsPlayerOne.Count() + matchesWonAsPlayerTwo.Count();
                int totalMatchesLost = matchesLostAsPlayerOne.Count() + matchesLostAsPlayerTwo.Count();

                standings.Add(
                    new SingleLeagueStandingsQuery(
                        userId,
                        leagueId,
                        totalMatchesWon,
                        totalMatchesLost,
                        await GetTotalGoalsScored(userId, leagueId),
                        await GetTotalGoalsRecieved(userId, leagueId),
                        Zero,
                        (totalMatchesLost + totalMatchesWon),
                        totalMatchesWon * Points,
                        userInfo.FirstName,
                        userInfo.LastName,
                        userInfo.Email
                    )
                );
            }

            var sortedLeague = ReturnSortedLeague(standings);
            var sortedLeagueWithPositions = AddPositionInLeagueToList(sortedLeague);

            return sortedLeagueWithPositions;
        }
    }
}
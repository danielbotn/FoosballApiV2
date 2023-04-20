using Dapper;
using FoosballApi.Dtos.Users;
using Npgsql;

namespace FoosballApi.Services
{
    public interface ISingleLeaguePlayersService
    {
        Task AddSingleLeaguePlayers(List<UserReadDto> users, int leagueId);
        Task StartLeague(int leagueId);
        Task<bool> HasPlayerAccessToLeague(int userId, int leagueId);
    }

    public class SingleLeaguePlayersService : ISingleLeaguePlayersService
    {
        private string _connectionString { get; }
        public SingleLeaguePlayersService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task AddSingleLeaguePlayers(List<UserReadDto> users, int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    
                        foreach (var user in users)
                        {
                            var leaguePlayer = new
                            {
                                user_id = user.Id,
                                league_id = leagueId,
                                created_at = DateTime.UtcNow
                            };

                            await conn.ExecuteAsync(
                                @"INSERT INTO league_players (user_id, league_id, created_at)
                                VALUES (@user_id, @league_id, @created_at)",
                                leaguePlayer,
                                transaction);
                        }

                        transaction.Commit();
                }
            }
        }

        public async Task StartLeague(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"UPDATE leagues SET has_league_started = @has_league_started WHERE id = @id",
                    new { has_league_started = true, id = leagueId });
            }
        }

        public async Task<bool> HasPlayerAccessToLeague(int userId, int leagueId)
        {
            bool result = false;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var userID = await conn.QueryFirstOrDefaultAsync<int?>(
                    @"SELECT user_id AS UserId
                    FROM league_players WHERE league_id = @league_id AND user_id = @user_id",
                    new { league_id = leagueId, user_id = userId });

                if (userID == null) {
                    result = false;
                }
                else
                {
                    result = true;
                } 
               
                
                return result;
            }
        }
    }
}
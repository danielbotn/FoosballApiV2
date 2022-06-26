using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FoosballApi.Models.DoubleLeaguePlayers;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IDoubleLeaguePlayerService
    {
        Task<IEnumerable<DoubleLeaguePlayerModelDapper>> GetDoubleLeaguePlayersyLeagueId(int leagueId);
        Task<DoubleLeaguePlayerModelDapper> GetDoubleLeaguePlayerById(int playerId);
    }

    public class DoubleLeaguePlayerService : IDoubleLeaguePlayerService
    {
        public string _connectionString { get; }

        public DoubleLeaguePlayerService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task<DoubleLeaguePlayerModelDapper> GetDoubleLeaguePlayerById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                 var dapperReadData = await conn.QueryFirstOrDefaultAsync<DoubleLeaguePlayerModelDapper>(
                        @"select dlp.id as Id, dlp.user_id as UserId, dlp.double_league_team_id as DoubleLeagueTeamId, u.first_name as FirstName, 
                        u.last_name as LastName, u.email as Email, dlt.id as TeamId, dlt.name as TeamName
                        from double_league_players dlp
                        join double_league_teams dlt on dlp.double_league_team_id = dlt.id
                        join users u on u.id = dlp.user_id " + $"where dlp.id = @id",
                            new { id });
                    return dapperReadData;
            }
        }

        public async Task<IEnumerable<DoubleLeaguePlayerModelDapper>> GetDoubleLeaguePlayersyLeagueId(int leagueId)
        {
           using (var conn = new NpgsqlConnection(_connectionString))
            {
                var dapperReadData = await conn.QueryAsync<DoubleLeaguePlayerModelDapper>(
                    @"select dlp.id, dlp.user_id, dlp.double_league_team_id DoubleLeagueTeamId, u.first_name FirstName, 
                    u.last_name LastName, u.email, dlt.id as teamId, dlt.name as team_name from double_league_players dlp
                    join double_league_teams dlt on dlp.double_league_team_id = dlt.id
                    join users u on u.id = dlp.user_id " + $"where dlt.league_id = @league_id",
                    new { league_id = leagueId });
                return dapperReadData.ToList();
            }
        }
    }
}
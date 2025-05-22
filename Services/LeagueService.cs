using FoosballApi.Models.Leagues;
using FoosballApi.Models;
using Npgsql;
using Dapper;

namespace FoosballApi.Services
{
    public interface ILeagueService
    {
        Task<IEnumerable<LeagueModel>> GetLeaguesByOrganisationId(int organisationId, int userId);
        Task<bool> CheckLeagueAccess(int userId, int organisationId);
        Task<int> GetOrganisationId(int leagueId);
        Task<LeagueModel> GetLeagueById(int id);
        void UpdateLeague(LeagueModel leagueModel);
        Task<IEnumerable<LeaguePlayersJoinModel>> GetLeaguesPlayers(int leagueId);
        Task<LeagueModel> CreateLeague(LeagueModelCreate leagueModelCreate);
        void DeleteLeague(LeagueModel leagueModel);
    }

    public class LeagueService : ILeagueService
    {
        private string _connectionString { get; }
        public LeagueService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        private async Task<List<OrganisationListModel>> GetOrganisationList(int userId, int organisationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var data = await conn.QueryAsync<OrganisationListModel>(
                    @"SELECT id as Id, organisation_id as OrganisationId, user_id as UserId
                    FROM organisation_list
                    WHERE user_id = @user_id AND organisation_id = @organisation_id",
                new { user_id = userId, organisation_id = organisationId });
                return data.ToList();
            }
        }

        public async Task<bool> CheckLeagueAccess(int userId, int organisationId)
        {
            var query = await GetOrganisationList(userId, organisationId);

            var data = query.ToList();
            if (data.Count == 0)
                return false;

            return true;
        }

        // We only give access to leagues that the user is part of
        public async Task<IEnumerable<LeagueModel>> GetLeaguesByOrganisationId(int organisationId, int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var leagues = await conn.QueryAsync<LeagueModel>(
                @"SELECT l.id as Id, l.name as Name, l.type_of_league as TypeOfLeague,
                    l.created_at as CreatedAt, l.up_to as UpTo, l.organisation_id as OrganisationId,
                    l.has_league_started as HasLeagueStarted, l.how_many_rounds as HowManyRounds
                    FROM leagues l
                    
                    WHERE l.organisation_id = @organisationId",
            new { organisationId, userId });
            return await CheckLeaguesStatus(leagues.ToList(), userId, organisationId);
        }

        private async Task<bool> CheckUserLeagueAccess(NpgsqlConnection conn, int leagueId, int userId, TypeOfLeague leagueType)
        {
            string query;
            if (leagueType == TypeOfLeague.single_league)
            {
                query = @"
                  SELECT COUNT(*) 
                  FROM league_players 
                  WHERE league_id = @LeagueId AND user_id = @UserId";
            }
            else // double league
            {
                query = @"
                  SELECT COUNT(*) 
                  FROM double_league_players dlp
                  JOIN double_league_teams dlt ON dlp.double_league_team_id = dlt.id
                  WHERE dlt.league_id = @LeagueId AND dlp.user_id = @UserId";
            }

            int? count = await conn.ExecuteScalarAsync<int?>(query, new { LeagueId = leagueId, UserId = userId });
            return count.HasValue && count.Value > 0;
        }

        private async Task<IEnumerable<LeagueModel>> CheckLeaguesStatus(IEnumerable<LeagueModel> leagues, int userId, int organisationId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            foreach (var league in leagues)
            {
                string matchCountQuery = league.TypeOfLeague == TypeOfLeague.single_league
                    ? "SELECT COUNT(*) FROM single_league_matches WHERE league_id = @LeagueId"
                    : "SELECT COUNT(*) FROM double_league_matches WHERE league_id = @LeagueId";

                int totalMatches = await conn.ExecuteScalarAsync<int>(matchCountQuery, new { LeagueId = league.Id });

                bool hasAccess = await CheckUserLeagueAccess(conn, league.Id, userId, league.TypeOfLeague);

                league.HasAccess = hasAccess;

                if (totalMatches == 0)
                {
                    league.HasLeagueEnded = false; // League hasn't started
                }
                else
                {
                    string unfinishedMatchQuery = league.TypeOfLeague == TypeOfLeague.single_league
                        ? "SELECT COUNT(*) FROM single_league_matches WHERE league_id = @LeagueId AND match_ended = false"
                        : "SELECT COUNT(*) FROM double_league_matches WHERE league_id = @LeagueId AND match_ended = false";

                    int unfinishedMatches = await conn.ExecuteScalarAsync<int>(unfinishedMatchQuery, new { LeagueId = league.Id });
                    league.HasLeagueEnded = unfinishedMatches == 0; // League has ended if no unfinished matches
                }
            }
            return leagues;
        }

        public async Task<int> GetOrganisationId(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<int>(
                    @"SELECT organisation_id as OrganisationId
                    FROM leagues
                    WHERE id = @league_id",
                new { league_id = leagueId });
                return query.FirstOrDefault();
            }
        }

        public async Task<LeagueModel> GetLeagueById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<LeagueModel>(
                    @"SELECT id as Id, name as Name, type_of_league as TypeOfLeague,
                    created_at as CreatedAt, up_to as UpTo, organisation_id as OrganisationId,
                    has_league_started as HasLeagueStarted, how_many_rounds as HowManyRounds
                    FROM leagues
                    WHERE id = @id",
                new { id = id });
                return query.FirstOrDefault();
            }
        }

        // type_of_league = leagueModel.TypeOfLeague, 
        // Dapper does not seem to have a way to update an enum value
        public void UpdateLeague(LeagueModel leagueModel)
        {
            int type_of_league = (int)leagueModel.TypeOfLeague;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE leagues
                    SET name = @name, up_to = @up_to,
                    has_league_started = @has_league_started, how_many_rounds = @how_many_rounds
                    WHERE id = @id",
                new 
                { 
                    name = leagueModel.Name, 
                    up_to = leagueModel.UpTo,
                    has_league_started = leagueModel.HasLeagueStarted, 
                    how_many_rounds = leagueModel.HowManyRounds, 
                    id = leagueModel.Id 
                });
            }
        }

        public async Task<IEnumerable<LeaguePlayersJoinModel>> GetLeaguesPlayers(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<LeaguePlayersJoinModel>(
                    @"SELECT lp.id as Id, lp.user_id as UserId, lp.league_id as LeagueId,
                    u.email as Email, u.first_name as FirstName, u.last_name as LastName
                    FROM league_players lp
                    JOIN users u ON lp.user_id = u.id
                    WHERE league_id = @league_id",
                new { league_id = leagueId });
                return query;
            }
        }

        public async Task<LeagueModel> CreateLeague(LeagueModelCreate leagueModelCreate)
        {
            DateTime now = DateTime.Now;
            LeagueModel result = new LeagueModel();
            result.Name = leagueModelCreate.Name;
            result.TypeOfLeague = leagueModelCreate.TypeOfLeague;
            result.CreatedAt = now;
            result.UpTo = leagueModelCreate.UpTo;
            result.OrganisationId = leagueModelCreate.OrganisationId;
            result.HasLeagueStarted = false;
            result.HowManyRounds = leagueModelCreate.HowManyRounds;

            var conn = new NpgsqlConnection(_connectionString);

            if (conn.State != System.Data.ConnectionState.Open)
            {
                conn.Open();
            }

            conn.TypeMapper.MapEnum<TypeOfLeague>("league_type");

            using (var cmd = new NpgsqlCommand(@"
                INSERT INTO leagues (name, type_of_league, created_at, up_to, organisation_id, has_league_started, how_many_rounds) 
                VALUES (@name, @type_of_league, @created_at, @up_to, @organisation_id, @has_league_started, @how_many_rounds) RETURNING id", conn))
            {
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "name",
                    Value = result.Name
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "type_of_league",
                    Value = result.TypeOfLeague
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "created_at",
                    Value = now
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "up_to",
                    Value = result.UpTo
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "organisation_id",
                    Value = result.OrganisationId
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "has_league_started",
                    Value = result.HasLeagueStarted
                });
                cmd.Parameters.Add(new NpgsqlParameter
                {
                    ParameterName = "how_many_rounds",
                    Value = result.HowManyRounds
                });
                var index = await cmd.ExecuteScalarAsync();
                result.Id = (int)index;
            }

            return result;
        }

        public void DeleteLeague(LeagueModel leagueModel)
        {
            if (leagueModel == null)
            {
                throw new ArgumentNullException(nameof(leagueModel));
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM leagues WHERE id = @id",
                    new { id = leagueModel.Id });
            }
        }
    }
}

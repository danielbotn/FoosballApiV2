using Dapper;
using FoosballApi.Dtos.SingleLeagueMatches;
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
        Task<IEnumerable<SingleLeagueMatchesQuery>> GetAllMatchesByLeagueId(int leagueId);
        Task<bool> CheckMatchPermission(int matchId, int userId);
        Task<SingleLeagueMatchModelExtended> GetSingleLeagueMatchByIdExtended(int matchId);
        Task<SingleLeagueMatchModel> GetSingleLeagueMatchById(int matchId);
        void UpdateSingleLeagueMatch(SingleLeagueMatchModel match);
        void ResetMatch(SingleLeagueMatchModel match);
        Task<List<SingleLeagueMatchModel>> CreateSingleLeagueMatches(CreateSingleLeagueMatchesBody body);
        Task<IEnumerable<SingleLeagueMatchesQuery>> GetAllMatchesByOrganisationId(int organisationId);
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

        private async Task<List<SingleLeagueMatchModel>> GetMatchesWonAsPlayerOne(int userId, int leagueId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_one = @player_one AND match_ended = true AND player_one_score > player_two_score AND league_id = @league_id",
                new { player_one = userId, league_id = leagueId });
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesWonAsPlayerTwo(int userId, int leagueId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_two = @player_two AND match_ended = true AND player_two_score > player_one_score AND league_id = @league_id",
                new { player_two = userId, league_id = leagueId });
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesLostAsPlayerOne(int userId, int leagueId)
        {
             using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_one = @player_one AND match_ended = true AND player_one_score < player_two_score AND league_id = @league_id",
                new { player_one = userId, league_id = leagueId});
                
                return matches.ToList();
            }
        }

        private async Task<List<SingleLeagueMatchModel>> GetMatchesLostAsPlayerTwo(int userId, int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id
                    FROM single_league_matches
                    WHERE player_two = @player_two AND match_ended = true AND player_two_score < player_one_score AND league_id = @league_id",
                new { player_two = userId, league_id = leagueId });
                
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
                var matchesWonAsPlayerOne = await GetMatchesWonAsPlayerOne(userId, leagueId);
                var matchesWonAsPlayerTwo = await GetMatchesWonAsPlayerTwo(userId, leagueId);

                var matchesLostAsPlayerOne = await GetMatchesLostAsPlayerOne(userId, leagueId);
                var matchesLostAsPlayerTwo = await GetMatchesLostAsPlayerTwo(userId, leagueId);

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

        public async Task<IEnumerable<SingleLeagueMatchesQuery>> GetAllMatchesByLeagueId(int leagueId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchesQuery>(
                    @"SELECT 
                        slm.id as Id, 
                        slm.player_one as PlayerOne, 
                        slm.player_two as PlayerTwo, 
                        slm.league_id as LeagueId, 
                        slm.start_time as StartTime, 
                        slm.end_time as EndTime,
                        slm.player_one_score as PlayerOneScore, 
                        slm.player_two_score as PlayerTwoScore, 
                        slm.match_ended as MatchEnded, 
                        slm.match_paused as MatchPaused, 
                        slm.match_started as MatchStarted,
                        l.organisation_id as OrganisationId,
                        (SELECT u.first_name FROM Users u WHERE u.id = slm.player_one) as PlayerOneFirstName,
                        (SELECT u2.last_name FROM Users u2 WHERE u2.id = slm.player_one) as PlayerOneLastName,
                        (SELECT u3.first_name FROM Users u3 WHERE u3.id = slm.player_two) as PlayerTwoFirstName,
                        (SELECT u4.last_name FROM Users u4 WHERE u4.id = slm.player_two) as PlayerTwoLastName,
                        (SELECT u5.photo_url FROM Users u5 WHERE u5.id = slm.player_one) as PlayerOnePhotoUrl,
                        (SELECT u6.photo_url FROM Users u6 WHERE u6.id = slm.player_two) as PlayerTwoPhotoUrl
                    FROM 
                        single_league_matches slm
                    JOIN 
                        leagues l ON l.id = slm.league_id
                    WHERE 
                        league_id = @league_id
                    ORDER BY 
                        CASE 
                            WHEN slm.match_ended = true THEN 0
                            ELSE 1
                        END,
                        slm.end_time DESC;
                    ",
                new { league_id = leagueId });
                
                return matches.ToList();
            }
        }

        public async Task<IEnumerable<SingleLeagueMatchesQuery>> GetAllMatchesByOrganisationId(int organisationId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var matches = await conn.QueryAsync<SingleLeagueMatchesQuery>(
                    @"SELECT slm.id as Id, slm.player_one as PlayerOne, slm.player_two as PlayerTwo, slm.league_id as LeagueId, 
                    slm.start_time as StartTime, slm.end_time as EndTime,
                    slm.player_one_score as PlayerOneScore, slm.player_two_score as PlayerTwoScore, 
                    slm.match_ended as MatchEnded, slm.match_paused as MatchPaused, slm.match_started as MatchStarted,
                    l.organisation_id as OrganisationId,
                    (SELECT u.first_name from Users u where u.id = slm.player_one) as PlayerOneFirstName,
                    (SELECT u2.last_name from Users u2 where u2.id = slm.player_one) as PlayerOneLastName,
                    (SELECT u3.first_name from Users u3 where u3.id = slm.player_two) as PlayerTwoFirstName,
                    (SELECT u4.last_name from Users u4 where u4.id = slm.player_two) as PlayerTwoLastName,
                    (SELECT u5.photo_url from Users u5 where u5.id = slm.player_one) as PlayerOnePhotoUrl,
                    (SELECT u6.photo_url from Users u6 where u6.id = slm.player_two) as PlayerTwoPhotoUrl
                    FROM single_league_matches slm
                    JOIN leagues l on l.id = slm.league_id
                    where l.organisation_id = @organisation_id And slm.match_ended = false AND slm.match_started = true


",
                new { organisation_id = organisationId });
                
                return matches.ToList();
            }
        }

        public async Task<bool> CheckMatchPermission(int matchId, int userId)
        {
            SingleLeagueMatchModel query;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var q = await conn.QueryFirstAsync<SingleLeagueMatchModel>(
                    @"SELECT id as Id, player_one as PlayerOne, player_two as PlayerTwo
                    FROM single_league_matches
                    WHERE id = @id AND player_one = @user_id OR player_one = @user_Id",
                new { id = matchId, user_id = userId,  });
                
               query = q;
            }

            if (query.PlayerOne == userId || query.PlayerTwo == userId)
                return true;

            return false;
        }

        private async Task<SingleLeagueMatchModelExtended> GetSingleLeagueMatchByIdData(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryFirstAsync<SingleLeagueMatchModelExtended>(
                    @"SELECT slm.id AS Id, slm.player_one As PlayerOne, slm.player_two AS PlayerTwo, 
                    slm.league_id AS LeagueId, slm.start_time AS StartTime, slm.end_time AS EndTime,
                    slm.player_one_score AS PlayerOneScore, slm.player_two_score AS PlayerTwoScore, 
                    slm.match_ended as MatchEnded, slm.match_paused AS MatchPaused, slm.match_started AS MatchStarted,
                    (SELECT u.first_name from Users u where u.id = slm.player_one) AS PlayerOneFirstName,
                    (SELECT u2.last_name from Users u2 where u2.id = slm.player_one) AS PlayerOneLastName,
                    (SELECT u2.photo_url from Users u2 where u2.id = slm.player_one) AS PlayerOnePhotoUrl,
                    (SELECT u3.first_name from Users u3 where u3.id = slm.player_two) as PlayerTwoFirstName,
                    (SELECT u4.last_name from Users u4 where u4.id = slm.player_two) as PlayerTwoLastName,
                    (SELECT u4.photo_url from Users u4 where u4.id = slm.player_two) as PlayerTwoPhotoUrl
                    FROM single_league_matches slm
                    WHERE id = @id",
                new { id = matchId,  });
                
               return query;
            }
        }

        public string ToReadableAgeString(TimeSpan span)
        {
            return string.Format("{0:hh\\:mm\\:ss}", span);
        }

        public async Task<SingleLeagueMatchModelExtended> GetSingleLeagueMatchByIdExtended(int matchId)
        {
            var data = await GetSingleLeagueMatchByIdData(matchId);

            TimeSpan? playingTime = null;
            if (data.EndTime != null) {
                playingTime = data.EndTime - data.StartTime;
            }
            SingleLeagueMatchModelExtended match = new SingleLeagueMatchModelExtended {
                Id = data.Id,
                PlayerOne = data.PlayerOne,
                PlayerOneFirstName = data.PlayerOneFirstName,
                PlayerOneLastName = data.PlayerOneLastName,
                PlayerOnePhotoUrl = data.PlayerOnePhotoUrl,
                PlayerTwo = data.PlayerTwo,
                PlayerTwoFirstName = data.PlayerTwoFirstName,
                PlayerTwoLastName = data.PlayerTwoLastName,
                PlayerTwoPhotoUrl = data.PlayerTwoPhotoUrl,
                LeagueId = data.LeagueId,
                StartTime = data.StartTime,
                EndTime = data.EndTime,
                PlayerOneScore = data.PlayerOneScore,
                PlayerTwoScore = data.PlayerTwoScore,
                MatchStarted = data.MatchStarted,
                MatchEnded = data.MatchEnded,
                MatchPaused = data.MatchPaused,
                TotalPlayingTime = playingTime != null ? ToReadableAgeString(playingTime.Value) : null,
            };
            return match;
        }

        public async Task<SingleLeagueMatchModel> GetSingleLeagueMatchById(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryFirstAsync<SingleLeagueMatchModel>(
                    @"SELECT slm.id AS Id, slm.player_one As PlayerOne, slm.player_two AS PlayerTwo, 
                    slm.league_id AS LeagueId, slm.start_time AS StartTime, slm.end_time AS EndTime,
                    slm.player_one_score AS PlayerOneScore, slm.player_two_score AS PlayerTwoScore, 
                    slm.match_ended as MatchEnded, slm.match_paused AS MatchPaused, slm.match_started AS MatchStarted
                    FROM single_league_matches slm
                    WHERE id = @id",
                new { id = matchId,  });
                
               return query;
            }
        }

        public void UpdateSingleLeagueMatch(SingleLeagueMatchModel match)
        {
            if (match.StartTime != null)
            {
                //var connection = new NtpConnection("pool.ntp.org");
                //var utcNow = connection.GetUtc();
                match.StartTime = DateTime.Now;
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE single_league_matches
                    SET player_one = @player_one, 
                    player_two = @player_two,
                    league_id = @league_id,
                    start_time = @start_time, 
                    end_time = @end_time,
                    player_one_score = @player_one_score,
                    player_two_score = @player_two_score,
                    match_started = @match_started,
                    match_ended = @match_ended,
                    match_paused = @match_paused
                    WHERE id = @id",
                new 
                { 
                    player_one = match.PlayerOne, 
                    player_two = match.PlayerTwo,
                    league_id = match.LeagueId,
                    start_time = match.StartTime,
                    end_time = match.EndTime,
                    player_one_score = match.PlayerOneScore,
                    player_two_score = match.PlayerTwoScore,
                    match_started = match.MatchStarted,
                    match_ended = match.MatchEnded,
                    match_paused = match.MatchPaused,
                    id = match.Id
                });
            }
        }

        private void DeleteAllGoalsByMatchId(int matchId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"DELETE FROM single_league_goals
                    WHERE match_id = @match_id",
                    new { match_id = matchId });
            }
        }

        private void ResetAllColumns(SingleLeagueMatchModel match)
        {
            SingleLeagueMatchModel emptyMatch = new SingleLeagueMatchModel
            {
                Id = match.Id,
                PlayerOne = match.PlayerOne,
                PlayerTwo = match.PlayerTwo,
                LeagueId = match.LeagueId,
                StartTime = null,
                EndTime = null,
                PlayerOneScore = 0,
                PlayerTwoScore = 0,
                MatchStarted = false,
                MatchEnded = false,
                MatchPaused = false
            };

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE single_league_matches
                    SET start_time = @start_time, 
                    end_time = @end_time,
                    player_one_score = @player_one_score,
                    player_two_score = @player_two_score,
                    match_started = @match_started,
                    match_ended = @match_ended,
                    match_paused = @match_paused
                    WHERE id = @id",
                new 
                { 
                    start_time = emptyMatch.StartTime,
                    end_time = emptyMatch.EndTime,
                    player_one_score = emptyMatch.PlayerOneScore,
                    player_two_score = emptyMatch.PlayerTwoScore,
                    match_started = emptyMatch.MatchStarted,
                    match_ended = emptyMatch.MatchEnded,
                    match_paused = emptyMatch.MatchPaused,
                    id = emptyMatch.Id
                });
            }
        }

        public void ResetMatch(SingleLeagueMatchModel match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            DeleteAllGoalsByMatchId(match.Id);

            ResetAllColumns(match);

        }

        public async Task<List<SingleLeagueMatchModel>> CreateSingleLeagueMatches(CreateSingleLeagueMatchesBody body)
        {
            // Check if howManyRounds parameter is null and retrieve it from the leagues table
            int howManyRounds = (int)(body.HowManyRounds ?? await GetHowManyRoundsFromLeague(body.LeagueId));

            // Retrieve all players for the given league
            var players = await GetPlayersForLeague(body.LeagueId);

            // Create single league matches
            var matches = GenerateSingleLeagueMatches(players, howManyRounds, body.LeagueId);

            // Insert matches into the database
            await InsertSingleLeagueMatches(matches);

            return matches;
        }

        private async Task<int?> GetHowManyRoundsFromLeague(int leagueId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var query = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT how_many_rounds FROM leagues WHERE id = @leagueId",
                new { leagueId });

            return query;
        }

        private async Task<List<LeaguePlayersModel>> GetPlayersForLeague(int leagueId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var query = await conn.QueryAsync<LeaguePlayersModel>(
                @"SELECT id as Id, user_id as UserId, league_id as LeagueId
                    FROM league_players
                    WHERE league_id = @league_id",
                new { league_id = leagueId });

            return query.ToList();
        }

        private static List<SingleLeagueMatchModel> GenerateSingleLeagueMatches(List<LeaguePlayersModel> players, int howManyRounds, int leagueId)
        {
            var matches = new List<SingleLeagueMatchModel>();

            for (int round = 1; round <= howManyRounds; round++)
            {
                for (int i = 0; i < players.Count - 1; i++)
                {
                    for (int j = i + 1; j < players.Count; j++)
                    {
                        var match = new SingleLeagueMatchModel
                        {
                            PlayerOne = players[i].UserId,
                            PlayerTwo = players[j].UserId,
                            LeagueId = leagueId,
                            StartTime = null,
                            EndTime = null,
                            PlayerOneScore = 0,
                            PlayerTwoScore = 0,
                            MatchStarted = false,
                            MatchEnded = false,
                            MatchPaused = false
                        };

                        matches.Add(match);
                    }
                }
            }

            return matches;
        }

        private async Task InsertSingleLeagueMatches(List<SingleLeagueMatchModel> matches)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                foreach (var match in matches)
                {
                    // Use RETURNING to get the ID of the newly inserted row
                    var insertedId = await conn.ExecuteScalarAsync<int>(
                        @"INSERT INTO single_league_matches 
                        (player_one, player_two, league_id, start_time, end_time, player_one_score, player_two_score, match_started, match_ended, match_paused)
                        VALUES (@PlayerOne, @PlayerTwo, @LeagueId, @StartTime, @EndTime, @PlayerOneScore, @PlayerTwoScore, @MatchStarted, @MatchEnded, @MatchPaused)
                        RETURNING id",
                        match, transaction);

                    // Update the match with the inserted ID
                    match.Id = insertedId;
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                // Handle the exception as needed (logging, rethrow, etc.)
                throw;
            }
        }
    }
}
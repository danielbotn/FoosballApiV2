using AutoMapper;
using FoosballApi.Hub;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Npgsql;

namespace FoosballApi.Services;

public interface IMatchesRealtimeService
{
    Task ListenForScoreUpdates(CancellationToken stoppingToken);
}

public class MatchesRealtimeService : IMatchesRealtimeService
{
    private readonly IHubContext<MessageHub> _hubContext;
    private readonly string _connectionString;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;

    public MatchesRealtimeService(IHubContext<MessageHub> hubContext, 
    string connectionString, 
    IHttpContextAccessor httpContextAccessor,
    IMapper mapper)
    {
        _hubContext = hubContext;
        _connectionString = connectionString;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
    }

    public Task ListenForDoubleScoreUpdates(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }

    public async Task ListenForScoreUpdates(CancellationToken stoppingToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(stoppingToken);

        // Handle incoming notifications
        connection.Notification += async (o, e) =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            try
            {
                if (e.Channel == "single_league_score_update")
                {
                    var updatedSingleLeagueMatch = JsonConvert.DeserializeObject<SingleLeagueMatchRealTime>(e.Payload);
                    var matchMapped = _mapper.Map<Match>(updatedSingleLeagueMatch);
                    string currentOrganisationId = updatedSingleLeagueMatch.OrganisationId.ToString();

                    if (!string.IsNullOrEmpty(currentOrganisationId))
                    {
                        // Broadcast the double match update to the specific organization group
                        await _hubContext.Clients.Group(currentOrganisationId).SendAsync("SendLiveMatches", matchMapped, stoppingToken);
                    }
                }
                
                if (e.Channel == "double_score_update")
                {
                    // Deserialize as FreehandDoubleMatchRealTime
                    var updatedDoubleMatch = JsonConvert.DeserializeObject<DoubleFreehandMatchRealTime>(e.Payload);
                    var doubleMatchMapped = _mapper.Map<Match>(updatedDoubleMatch);
                    string currentOrganisationId = updatedDoubleMatch.OrganisationId.ToString();

                    if (!string.IsNullOrEmpty(currentOrganisationId))
                    {
                        // Broadcast the double match update to the specific organization group
                        await _hubContext.Clients.Group(currentOrganisationId).SendAsync("SendLiveMatches", doubleMatchMapped, stoppingToken);
                    }
                }
                else if (e.Channel == "score_update")
                {
                    // Deserialize as FreehandMatchRealTime
                    var updatedMatch = JsonConvert.DeserializeObject<FreehandMatchRealTime>(e.Payload);
                    var matchMapped = _mapper.Map<Match>(updatedMatch);
                    string currentOrganisationId = updatedMatch.OrganisationId.ToString();

                    if (!string.IsNullOrEmpty(currentOrganisationId))
                    {
                        // Broadcast the single match update to the specific organization group
                        await _hubContext.Clients.Group(currentOrganisationId).SendAsync("SendLiveMatches", matchMapped, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging purposes
                Console.WriteLine($"Error processing notification: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                // You can also log the payload for further inspection
                Console.WriteLine($"Payload: {e.Payload}");
            }
        };

        // Listen to both 'score_update' and 'double_score_update'
        using (var cmd = new NpgsqlCommand("LISTEN score_update; LISTEN double_score_update; Listen double_score_insert; LISTEN single_league_score_update;", connection))
        {
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }

        // Wait for notifications
        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
        }
    }

}

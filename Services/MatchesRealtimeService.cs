using FoosballApi.Hub;
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

    public MatchesRealtimeService(IHubContext<MessageHub> hubContext, 
    string connectionString, 
    IHttpContextAccessor httpContextAccessor)
    {
        _hubContext = hubContext;
        _connectionString = connectionString;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task ListenForScoreUpdates(CancellationToken stoppingToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(stoppingToken);

        connection.Notification += async (o, e) =>
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            var updatedMatch = JsonConvert.DeserializeObject<FreehandMatchRealTime>(e.Payload);
            string currentOrganisationId = updatedMatch.OrganisationId.ToString();

            if (!string.IsNullOrEmpty(currentOrganisationId))
            {
                // Broadcast to the specific organization group
                await _hubContext.Clients.Group(currentOrganisationId).SendAsync("UpdateScore", updatedMatch, stoppingToken);
            }
        };

        using (var cmd = new NpgsqlCommand("LISTEN score_update;", connection))
        {
            await cmd.ExecuteNonQueryAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await connection.WaitAsync(stoppingToken);
        }
    }
}

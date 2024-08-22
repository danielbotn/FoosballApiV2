namespace FoosballApi.Services;
public class ScoreNotificationBackgroundService(IMatchesRealtimeService matchesRealtimeService) : BackgroundService
{
    private readonly IMatchesRealtimeService _matchesRealtimeService = matchesRealtimeService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _matchesRealtimeService.ListenForScoreUpdates(stoppingToken);
    }
}


using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Other;
using System.Text;

namespace FoosballApi.Services
{
    public interface IMicrosoftTeamsService
    {
        Task SendTeamsMessageForFreehandGame(FreehandMatchModel match, int userId);
        Task SendTeamsMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId);
        Task SendTeamsMessageForSingleLeague(SingleLeagueMatchModel match, int userId);
        Task SendTeamsMessageForDoubleLeague(DoubleLeagueMatchModel match, int userId);
    }

    public class MicrosoftTeamsService : IMicrosoftTeamsService
    {
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        private readonly ILeagueService _leagueService;
        private readonly IDoubleLeagueTeamService _doubleLeagueTeamService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;


        public MicrosoftTeamsService(
            ISingleLeagueMatchService singleLeagueMatchService,
            IUserService userService,
            IOrganisationService organisationService,
            ILeagueService leagueService,
            IDoubleLeagueTeamService doubleLeagueTeamService,
            IDoubleLeaugeMatchService doubleLeagueMatchService
           )
        {
            _singleLeagueMatchService = singleLeagueMatchService;
            _userService = userService;
            _organisationService = organisationService;
            _leagueService = leagueService;
            _doubleLeagueTeamService = doubleLeagueTeamService;
            _doubleLeagueMatchService = doubleLeagueMatchService;
        }

        public async Task SendTeamsMessageForFreehandGame(FreehandMatchModel match, int userId)
        {
            HttpClient httpClient = new();
            
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.MicrosoftTeamsWebhookUrl))
                {
                    _webhookUrl = data.MicrosoftTeamsWebhookUrl;
                }
            }

            User playerOne = await _userService.GetUserById(match.PlayerOneId);
            User playerTwo = await _userService.GetUserById(match.PlayerTwoId);
            bool isPlayerOneWinner = match.PlayerOneScore > match.PlayerTwoScore;
            User winner = isPlayerOneWinner ? playerOne : playerTwo;
            User loser = isPlayerOneWinner ? playerTwo : playerOne;
            int winnerScore = isPlayerOneWinner ? match.PlayerOneScore : match.PlayerTwoScore;
            int loserScore = isPlayerOneWinner ? match.PlayerTwoScore : match.PlayerOneScore;
            TimeSpan matchDuration = match.EndTime.HasValue ? match.EndTime.Value - match.StartTime : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            var content = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                contentUrl = (string)null,
                content = new
                {
                    schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                    type = "AdaptiveCard",
                    version = "1.2",
                    body = new object[]
                    {
                        new
                        {
                            type = "Container",
                            items = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = "⚽ Dano Game Result",
                                    weight = "Bolder",
                                    size = "Large",
                                    color = "Accent"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"{winner.FirstName} {winner.LastName} wins!",
                                    weight = "Bolder",
                                    size = "Medium"
                                },
                                new
                                {
                                    type = "Image",
                                    url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                                    size = "Small",
                                    style = "Person"
                                }
                            }
                        },
                        new
                        {
                            type = "FactSet",
                            facts = new object[]
                            {
                                new { title = "Winner", value = $"{winner.FirstName} {winner.LastName}" },
                                new { title = "Loser", value = $"{loser.FirstName} {loser.LastName}" },
                                new { title = "Final Score", value = $"{winnerScore} - {loserScore}" },
                                new { title = "Match Duration", value = formattedDuration }
                            }
                        },
                        new
                        {
                            type = "TextBlock",
                            text = $"Match ended at {(match.EndTime ?? DateTime.UtcNow):yyyy-MM-dd HH:mm:ss} UTC",
                            size = "Small",
                            isSubtle = true
                        },
                        new
                        {
                            type = "TextBlock",
                            text = "Powered by Dano Foosball",
                            size = "Small",
                            weight = "Bolder",
                            color = "Accent"
                        }
                    }
                }
            }
            }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_webhookUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds} seconds";
            if (duration.TotalHours < 1)
                return $"{(int)duration.TotalMinutes} minutes";
            return $"{(int)duration.TotalHours} hours and {duration.Minutes} minutes";
        }

        public async Task SendTeamsMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId)
        {
            HttpClient httpClient = new();

            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.MicrosoftTeamsWebhookUrl))
                {
                    _webhookUrl = data.MicrosoftTeamsWebhookUrl;
                }
            }

            User playerOneTeamA = await _userService.GetUserById(match.PlayerOneTeamA);
            User playerTwoTeamA = match.PlayerTwoTeamA.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamA.Value) : null;
            User playerOneTeamB = await _userService.GetUserById(match.PlayerOneTeamB);
            User playerTwoTeamB = match.PlayerTwoTeamB.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamB.Value) : null;

            bool isTeamAWinner = match.TeamAScore > match.TeamBScore;
            string winnerTeam = isTeamAWinner
                ? $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}{(playerTwoTeamA != null ? " & " + playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "")}"
                : $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}{(playerTwoTeamB != null ? " & " + playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "")}";
            string loserTeam = isTeamAWinner
                ? $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}{(playerTwoTeamB != null ? " & " + playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "")}"
                : $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}{(playerTwoTeamA != null ? " & " + playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "")}";
            int winnerScore = isTeamAWinner ? match.TeamAScore.GetValueOrDefault() : match.TeamBScore.GetValueOrDefault();
            int loserScore = isTeamAWinner ? match.TeamBScore.GetValueOrDefault() : match.TeamAScore.GetValueOrDefault();

            TimeSpan matchDuration = match.EndTime.HasValue ? match.EndTime.Value - match.StartTime.Value : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            var content = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                contentUrl = (string)null,
                content = new
                {
                    schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                    type = "AdaptiveCard",
                    version = "1.2",
                    body = new object[]
                    {
                        new
                        {
                            type = "Container",
                            items = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = "⚽ Dano Game Result",
                                    weight = "Bolder",
                                    size = "Large",
                                    color = "Accent"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"{winnerTeam} wins!",
                                    weight = "Bolder",
                                    size = "Medium"
                                },
                                new
                                {
                                    type = "Image",
                                    url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                                    size = "Small",
                                    style = "Person"
                                }
                            }
                        },
                        new
                        {
                            type = "FactSet",
                            facts = new object[]
                            {
                                new { title = "Winner Team", value = winnerTeam },
                                new { title = "Loser Team", value = loserTeam },
                                new { title = "Final Score", value = $"{winnerScore} - {loserScore}" },
                                new { title = "Match Duration", value = formattedDuration }
                            }
                        },
                        new
                        {
                            type = "TextBlock",
                            text = $"Match ended at {(match.EndTime ?? DateTime.UtcNow):yyyy-MM-dd HH:mm:ss} UTC",
                            size = "Small",
                            isSubtle = true
                        },
                        new
                        {
                            type = "TextBlock",
                            text = "Powered by Dano Foosball",
                            size = "Small",
                            weight = "Bolder",
                            color = "Accent"
                        }
                    }
                }
            }
        }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_webhookUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");
            }
        }

        public async Task SendTeamsMessageForSingleLeague(SingleLeagueMatchModel match, int userId)
        {
            HttpClient httpClient = new();

            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.MicrosoftTeamsWebhookUrl))
                {
                    _webhookUrl = data.MicrosoftTeamsWebhookUrl;
                }
            }

            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new Exception("Microsoft Teams webhook URL not found for the organisation.");
            }

            var embedContent = await GenerateSingleLeagueEmbed(match);

            var content = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                contentUrl = (string)null,
                content = new
                {
                    schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                    type = "AdaptiveCard",
                    version = "1.2",
                    body = new object[]
                    {
                        new
                        {
                            type = "Container",
                            items = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = "⚽ Dano Game Result",
                                    weight = "Bolder",
                                    size = "Large",
                                    color = "Accent"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"{embedContent["winnerName"]} wins!",
                                    weight = "Bolder",
                                    size = "Medium"
                                },
                                new
                                {
                                    type = "Image",
                                    url = embedContent["winnerIconUrl"],
                                    size = "Small",
                                    style = "Person"
                                }
                            }
                        },
                        new
                        {
                            type = "FactSet",
                            facts = embedContent["fields"]
                        },
                        new
                        {
                            type = "TextBlock",
                            text = $"Match ended at {(match.EndTime ?? DateTime.UtcNow):yyyy-MM-dd HH:mm:ss} UTC",
                            size = "Small",
                            isSubtle = true
                        },
                        new
                        {
                            type = "TextBlock",
                            text = "Powered by Dano Foosball",
                            size = "Small",
                            weight = "Bolder",
                            color = "Accent"
                        }
                    }
                }
            }
        }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_webhookUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");
            }
        }

        private async Task<Dictionary<string, object>> GenerateSingleLeagueEmbed(SingleLeagueMatchModel match)
        {
            User playerOne = await _userService.GetUserById(match.PlayerOne);
            User playerTwo = await _userService.GetUserById(match.PlayerTwo);
            bool isPlayerOneWinner = match.PlayerOneScore > match.PlayerTwoScore;
            User winner = isPlayerOneWinner ? playerOne : playerTwo;
            User loser = isPlayerOneWinner ? playerTwo : playerOne;
            int winnerScore = isPlayerOneWinner ? match.PlayerOneScore.GetValueOrDefault() : match.PlayerTwoScore.GetValueOrDefault();
            int loserScore = isPlayerOneWinner ? match.PlayerTwoScore.GetValueOrDefault() : match.PlayerOneScore.GetValueOrDefault();

            TimeSpan matchDuration = (match.EndTime.HasValue && match.StartTime.HasValue)
                ? match.EndTime.Value - match.StartTime.Value
                : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            var leagueStandings = await _singleLeagueMatchService.GetSigleLeagueStandings(match.LeagueId);
            var standingsText = GeneratePlainTextStandings(leagueStandings.ToList());

            var fields = new List<object>
    {
        new { title = "Winner", value = $"{winner.FirstName} {winner.LastName}" },
        new { title = "Loser", value = $"{loser.FirstName} {loser.LastName}" },
        new { title = "Final Score", value = $"{winnerScore} - {loserScore}" },
        new { title = "Match Duration", value = formattedDuration },
        new { title = "League Standings", value = standingsText }
    };

            return new Dictionary<string, object>
    {
        { "winnerName", $"{winner.FirstName} {winner.LastName}" },
        { "winnerIconUrl", winner.PhotoUrl },
        { "fields", fields }
    };
        }

        private string GeneratePlainTextStandings(List<SingleLeagueStandingsQuery> leagueData)
        {
            var sb = new StringBuilder();
            foreach (var item in leagueData)
            {
                sb.AppendLine($"{item.PositionInLeague}. {item.FirstName} {item.LastName} - {item.Points} pts");
            }
            return sb.ToString();
        }

        public async Task SendTeamsMessageForDoubleLeague(DoubleLeagueMatchModel match, int userId)
        {
            HttpClient httpClient = new();

            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.MicrosoftTeamsWebhookUrl))
                {
                    _webhookUrl = data.MicrosoftTeamsWebhookUrl;
                }
            }

            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new Exception("Microsoft Teams webhook URL not found for the organisation.");
            }

            var teamOne = _doubleLeagueTeamService.GetDoubleLeagueTeamById(match.TeamOneId);
            var teamTwo = _doubleLeagueTeamService.GetDoubleLeagueTeamById(match.TeamTwoId);

            string winnerTeam;
            string loserTeam;
            int winnerScore;
            int loserScore;
            if (match.TeamOneScore > match.TeamTwoScore)
            {
                winnerTeam = teamOne.Name;
                loserTeam = teamTwo.Name;
                winnerScore = match.TeamOneScore.GetValueOrDefault();
                loserScore = match.TeamTwoScore.GetValueOrDefault();
            }
            else
            {
                winnerTeam = teamTwo.Name;
                loserTeam = teamOne.Name;
                winnerScore = match.TeamTwoScore.GetValueOrDefault();
                loserScore = match.TeamOneScore.GetValueOrDefault();
            }

            TimeSpan matchDuration = match.EndTime.HasValue && match.StartTime.HasValue
                ? match.EndTime.Value - match.StartTime.Value
                : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            // Get league standings.
            var leagueStandings = GenerateDoubleLeagueStandings(match);

            var content = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                contentUrl = (string)null,
                content = new
                {
                    schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                    type = "AdaptiveCard",
                    version = "1.2",
                    body = new object[]
                    {
                        new
                        {
                            type = "Container",
                            items = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = "⚽ Dano Game Result",
                                    weight = "Bolder",
                                    size = "Large",
                                    color = "Accent"
                                },
                                new
                                {
                                    type = "TextBlock",
                                    text = $"{winnerTeam} wins!",
                                    weight = "Bolder",
                                    size = "Medium"
                                },
                                new
                                {
                                    type = "Image",
                                    url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                                    size = "Small",
                                    style = "Person"
                                }
                            }
                        },
                        new
                        {
                            type = "FactSet",
                            facts = new object[]
                            {
                                new { title = "Winner Team", value = winnerTeam },
                                new { title = "Loser Team", value = loserTeam },
                                new { title = "Final Score", value = $"{winnerScore} - {loserScore}" },
                                new { title = "Match Duration", value = formattedDuration },
                                new { title = "League Standings", value = leagueStandings }
                            }
                        },
                        new
                        {
                            type = "TextBlock",
                            text = $"Match ended at {(match.EndTime ?? DateTime.UtcNow):yyyy-MM-dd HH:mm:ss} UTC",
                            size = "Small",
                            isSubtle = true
                        },
                        new
                        {
                            type = "TextBlock",
                            text = "Powered by Dano Foosball",
                            size = "Small",
                            weight = "Bolder",
                            color = "Accent"
                        }
                    }
                }
            }
        }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(content);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(_webhookUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to send message. Status code: {response.StatusCode}");
                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response body: {responseBody}");
            }
        }

        private string GenerateDoubleLeagueStandings(DoubleLeagueMatchModel match)
        {
            var leagueData = _doubleLeagueMatchService.GetDoubleLeagueStandings(match.LeagueId).Result;
            var sb = new StringBuilder();
            var sortedData = leagueData.OrderByDescending(item => item.Points);
            foreach (var item in sortedData)
            {
                sb.AppendLine($"{item.PositionInLeague}. {item.TeamName} - {item.Points} pts");
            }
            return sb.ToString();
        }
    }
}

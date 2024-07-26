using FoosballApi.Helpers;
using FoosballApi.Models.Matches;
using FoosballApi.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using FoosballApi.Models.Other;
using TextTableFormatter;
using FoosballApi.Models.DoubleLeagueMatches;
using System.Text;
using FoosballApi.Models.DoubleLeagueTeams;

namespace FoosballApi.Services
{
    public interface ISlackService
    {
        Task SendSlackMessageForSingleLeague(SingleLeagueMatchModel match, int userId);
        Task SendSlackMessageForFreehandGame(FreehandMatchModel match, int userId);
        Task SendSlackMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId);
        Task SendSlackMessageForDoubleLeague(DoubleLeagueMatchModel match, int userId);
    }

    public class SlackService : ISlackService
    {
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        private readonly ILeagueService _leagueService;
        private readonly IDoubleLeagueTeamService _doubleLeagueTeamService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;
        

        public SlackService(
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

        public async Task SendSlackMessageForSingleLeague(SingleLeagueMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    _webhookUrl = data.SlackWebhookUrl;
                }
            }
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new Exception("Slack webhook URL not found for the organisation.");
            }

            var message = await GenerateSingleLeagueMessage(match);
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCallSlack(jsonPayload, _webhookUrl);
        }

        private async Task<object> GenerateSingleLeagueMessage(SingleLeagueMatchModel match)
        {
            User winner = match.PlayerOneScore > match.PlayerTwoScore
                ? await _userService.GetUserById(match.PlayerOne)
                : await _userService.GetUserById(match.PlayerTwo);
            User loser = match.PlayerOneScore > match.PlayerTwoScore
                ? await _userService.GetUserById(match.PlayerTwo)
                : await _userService.GetUserById(match.PlayerOne);
            int winnerScore = Math.Max(match.PlayerOneScore.GetValueOrDefault(), match.PlayerTwoScore.GetValueOrDefault());
            int loserScore = Math.Min(match.PlayerOneScore.GetValueOrDefault(), match.PlayerTwoScore.GetValueOrDefault());

            TimeSpan matchDuration = (match.EndTime.HasValue && match.StartTime.HasValue)
                ? match.EndTime.Value - match.StartTime.Value
                : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            var leagueStandings = await _singleLeagueMatchService.GetSigleLeagueStandings(match.LeagueId);
            var standingsText = GeneratePlainTextStandings(leagueStandings.ToList());
            
            var message = new
            {
                attachments = new[]
                {
                    new
                    {
                        color = "#36a64f",  // Green color for left border
                        blocks = new List<object>
                        {
                            new
                            {
                                type = "header",
                                text = new
                                {
                                    type = "plain_text",
                                    text = "⚽ Dano Game Result",
                                    emoji = true
                                }
                            },
                            new
                            {
                                type = "section",
                                text = new
                                {
                                    type = "mrkdwn",
                                    text = $":trophy: *{winner.FirstName} {winner.LastName} wins!*"
                                }
                            },
                            new
                            {
                                type = "section",
                                fields = new List<object>
                                {
                                    new { type = "mrkdwn", text = $"*Winner:*\n{winner.FirstName} {winner.LastName}" },
                                    new { type = "mrkdwn", text = $"*Loser:*\n{loser.FirstName} {loser.LastName}" },
                                    new { type = "mrkdwn", text = $"*Final Score:*\n{winnerScore} - {loserScore}" },
                                    new { type = "mrkdwn", text = $"*Match Duration:*\n{formattedDuration}" },
                                    new { type = "mrkdwn", text = $"*League Standings:*\n{standingsText}" }
                                }
                            },
                            new
                            {
                                type = "context",
                                elements = new List<object>
                                {
                                    new
                                    {
                                        type = "image",
                                        image_url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                                        alt_text = "Dano Foosball logo"
                                    },
                                    new
                                    {
                                        type = "mrkdwn",
                                        text = "Powered by Dano Foosball"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return message;
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

        // Maybe we will use this in production. Seems slow
        private async static Task<string> GetAIMessage(SingleLeagueMatchModel match, User userOne, User userTwo)
        {
            string result = "";
            string userPrompt = $"{userOne.FirstName} ${userOne.LastName} and ${userTwo.FirstName} ${userTwo.LastName} played a foosball match. " +
                $"${userOne.FirstName}  ${userOne.LastName} scored ${match.PlayerOneScore} goals and " +
                $"${userTwo.FirstName} ${userTwo.LastName} scored ${match.PlayerTwoScore} goals. " +
                $"Write a newspaper headline for the match. I only want one sentence. Don't give me options or anything other then the headline.";
            // Create a kernel with OpenAI chat completion
            #pragma warning disable SKEXP0010
            Kernel kernel = Kernel.CreateBuilder()
                                .AddOpenAIChatCompletion(
                                    modelId: "phi3:mini",
                                    endpoint: new Uri("http://localhost:11434"),
                                    apiKey: "")
                                .Build();

            var aiChatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.Add(new ChatMessageContent(AuthorRole.User, userPrompt));

            // Stream the AI response and add to chat history

            var response = "";
            await foreach (var item in
                aiChatService.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                Console.Write(item.Content);
                result += item.Content;
            }
            chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));

            return result;
        }

        private string FormatNames(SingleLeagueStandingsQuery item, int maxLength)
        {
            string position = item.FirstName.ToString() + " " + item.LastName.ToString();
            return position.PadRight(maxLength, ' ');
        }

        private string GenerateAsciiTable(List<SingleLeagueStandingsQuery> leagueData)
        {
            // Determine the maximum length of the combined FirstName and LastName strings
            int maxLength = leagueData.Max(item => (item.FirstName + " " + item.LastName).Length);
            maxLength = maxLength < 15 ? 15 : maxLength; // Ensure a minimum width for the Player column

            // Create a new TextTable with the number of columns
            var table = new TextTable(8, TableBordersStyle.DESIGN_FORMAL, TableVisibleBorders.SURROUND_HEADER_FOOTER_AND_COLUMNS);

            // Set fixed column width ranges
            table.SetColumnWidthRange(0, 2, 2);    // P
            table.SetColumnWidthRange(1, maxLength, maxLength); // Player
            table.SetColumnWidthRange(2, 2, 2);    // MP
            table.SetColumnWidthRange(3, 2, 2);    // MW
            table.SetColumnWidthRange(4, 2, 2);    // ML
            table.SetColumnWidthRange(5, 2, 2);    // GS
            table.SetColumnWidthRange(6, 2, 2);    // GR
            table.SetColumnWidthRange(7, 6, 6);    // Points

            // Add table headers
            table.AddCell("P");
            table.AddCell("Player");
            table.AddCell("MP");
            table.AddCell("MW");
            table.AddCell("ML");
            table.AddCell("GS");
            table.AddCell("GR");
            table.AddCell("Points");

            // Add rows for each league data entry
            foreach (var item in leagueData)
            {
                table.AddCell(item.PositionInLeague.ToString());
                table.AddCell(FormatNames(item, maxLength));
                table.AddCell(item.MatchesPlayed.ToString());
                table.AddCell(item.TotalMatchesWon.ToString());
                table.AddCell(item.TotalMatchesLost.ToString());
                table.AddCell(item.TotalGoalsScored.ToString());
                table.AddCell(item.TotalGoalsRecieved.ToString());
                table.AddCell(item.Points.ToString());
            }

            // Render the table to a string and return it

            return table.Render();
        }

        private async Task<string>GenerateSingleLeagueTable(SingleLeagueMatchModel match)
        {
            var leagueData = await _leagueService.GetLeagueById(match.LeagueId);
            var leagueStandings = await _singleLeagueMatchService.GetSigleLeagueStandings(match.LeagueId);
            var ascciiTable = leagueData.Name + "\n" + GenerateAsciiTable(leagueStandings.ToList());

            return ascciiTable;
        }

       

        private async static Task<string> GetAIMessage(FreehandMatchModel match, User userOne, User userTwo)
        {
            string result = "";
            string userPrompt = $"{userOne.FirstName} ${userOne.LastName} and ${userTwo.FirstName} ${userTwo.LastName} played a foosball match. " +
                $"${userOne.FirstName}  ${userOne.LastName} scored ${match.PlayerOneScore} goals and " +
                $"${userTwo.FirstName} ${userTwo.LastName} scored ${match.PlayerTwoScore} goals. " +
                $"Write a newspaper headline for the match. I only want one sentence. Don't give me options or anything other then the headline.";
            // Create a kernel with OpenAI chat completion
            #pragma warning disable SKEXP0010
            Kernel kernel = Kernel.CreateBuilder()
                                .AddOpenAIChatCompletion(
                                    modelId: "llama3:latest",
                                    endpoint: new Uri("http://localhost:11434"),
                                    apiKey: "")
                                .Build();

            var aiChatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.Add(new ChatMessageContent(AuthorRole.User, userPrompt));

            // Stream the AI response and add to chat history

            var response = "";
            await foreach (var item in
                aiChatService.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                Console.Write(item.Content);
                result += item.Content;
            }
            chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));

            return result;
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
            {
                return $"{duration.Seconds} seconds";
            }
            else if (duration.TotalHours < 1)
            {
                return $"{(int)duration.TotalMinutes} minutes";
            }
            else
            {
                return $"{(int)duration.TotalHours} hours and {(int)duration.Minutes} minutes";
            }
        }

        public async Task SendSlackMessageForFreehandGame(FreehandMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    _webhookUrl = data.SlackWebhookUrl;
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

            var blocks = new List<object>
            {
                new
                {
                    type = "divider"
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $":trophy: *{winner.FirstName} {winner.LastName} wins!*"
                    }
                },
                new
                {
                    type = "section",
                    fields = new List<object>
                    {
                        new { type = "mrkdwn", text = $"*Winner:*\n<{winner.PhotoUrl}|{winner.FirstName} {winner.LastName}>" },
                        new { type = "mrkdwn", text = $"*Loser:*\n<{loser.PhotoUrl}|{loser.FirstName} {loser.LastName}>" },
                        new { type = "mrkdwn", text = $"*Final Score:*\n{winnerScore} - {loserScore}" },
                        new { type = "mrkdwn", text = $"*Match Duration:*\n{formattedDuration}" }
                    }
                },
                new
                {
                    type = "context",
                    elements = new List<object>
                    {
                        new
                        {
                            type = "image",
                            image_url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                            alt_text = "Dano Foosball logo"
                        },
                        new
                        {
                            type = "mrkdwn",
                            text = "Powered by Dano Foosball"
                        }
                    }
                }
            };

            var message = new
            {
                blocks = new List<object>
                {
                    new
                    {
                        type = "section",
                        text = new
                        {
                            type = "mrkdwn",
                            text = "*⚽ Dano Game Result*"
                        }
                    }
                },
                        attachments = new List<object>
                {
                    new
                    {
                        color = "#36a64f",
                        blocks = blocks
                    }
                }
            };

            string bodyParam = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCallSlack(bodyParam, _webhookUrl);
        }


        private async static Task<string> GetAIMessage(FreehandDoubleMatchModel match, User playerOneTeamA, User playerTwoTeamA, User playerOneTeamB, User playerTwoTeamB)
        {
            string result = "";
            string userPrompt = $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName} and " +
                                $"{(playerTwoTeamA != null ? playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "N/A")} " +
                                $"played against {playerOneTeamB.FirstName} {playerOneTeamB.LastName} and " +
                                $"{(playerTwoTeamB != null ? playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "N/A")} in a foosball match. " +
                                $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName} and " +
                                $"{(playerTwoTeamA != null ? playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "N/A")} scored {match.TeamAScore} goals, " +
                                $"while {playerOneTeamB.FirstName} {playerOneTeamB.LastName} and " +
                                $"{(playerTwoTeamB != null ? playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "N/A")} scored {match.TeamBScore} goals. " +
                                $"Write a newspaper headline for the match. I only want one sentence. Don't give me options or anything other than the headline.";

            // Create a kernel with OpenAI chat completion
            #pragma warning disable SKEXP0010
            Kernel kernel = Kernel.CreateBuilder()
                                .AddOpenAIChatCompletion(
                                    modelId: "phi3:mini",
                                    endpoint: new Uri("http://localhost:11434"),
                                    apiKey: "")
                                .Build();

            var aiChatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.Add(new ChatMessageContent(AuthorRole.User, userPrompt));

            // Stream the AI response and add to chat history
            var response = "";
            await foreach (var item in
                aiChatService.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                Console.Write(item.Content);
                result += item.Content;
            }
            chatHistory.Add(new ChatMessageContent(AuthorRole.Assistant, response));

            return result;
        }

        public async Task SendSlackMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    _webhookUrl = data.SlackWebhookUrl;
                }
            }

            User playerOneTeamA = await _userService.GetUserById(match.PlayerOneTeamA);
            User playerTwoTeamA = match.PlayerTwoTeamA.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamA.Value) : null;
            User playerOneTeamB = await _userService.GetUserById(match.PlayerOneTeamB);
            User playerTwoTeamB = match.PlayerTwoTeamB.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamB.Value) : null;

            bool isTeamAWinner = match.TeamAScore > match.TeamBScore;
            string winnerTeam = isTeamAWinner
                ? $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}{(playerTwoTeamA != null ? $" & {playerTwoTeamA.FirstName} {playerTwoTeamA.LastName}" : "")}"
                : $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}{(playerTwoTeamB != null ? $" & {playerTwoTeamB.FirstName} {playerTwoTeamB.LastName}" : "")}";
            string loserTeam = isTeamAWinner
                ? $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}{(playerTwoTeamB != null ? $" & {playerTwoTeamB.FirstName} {playerTwoTeamB.LastName}" : "")}"
                : $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}{(playerTwoTeamA != null ? $" & {playerTwoTeamA.FirstName} {playerTwoTeamA.LastName}" : "")}";
            int winnerScore = isTeamAWinner ? match.TeamAScore.GetValueOrDefault() : match.TeamBScore.GetValueOrDefault();
            int loserScore = isTeamAWinner ? match.TeamBScore.GetValueOrDefault() : match.TeamAScore.GetValueOrDefault();

            TimeSpan matchDuration = match.EndTime.HasValue && match.StartTime.HasValue ? match.EndTime.Value - match.StartTime.Value : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);

            var blocks = new List<object>
            {
                new
                {
                    type = "divider"
                },
                new
                {
                    type = "section",
                    text = new
                    {
                        type = "mrkdwn",
                        text = $":trophy: *{winnerTeam} wins!*"
                    }
                },
                new
                {
                    type = "section",
                    fields = new List<object>
                    {
                        new { type = "mrkdwn", text = $"*Winner Team:*\n{winnerTeam}" },
                        new { type = "mrkdwn", text = $"*Loser Team:*\n{loserTeam}" },
                        new { type = "mrkdwn", text = $"*Final Score:*\n{winnerScore} - {loserScore}" },
                        new { type = "mrkdwn", text = $"*Match Duration:*\n{formattedDuration}" }
                    }
                },
                new
                {
                    type = "context",
                    elements = new List<object>
                    {
                        new
                        {
                            type = "image",
                            image_url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                            alt_text = "Dano Foosball logo"
                        },
                        new
                        {
                            type = "mrkdwn",
                            text = "Powered by Dano Foosball"
                        }
                    }
                }
            };

                    var message = new
                    {
                        blocks = new List<object>
                {
                    new
                    {
                        type = "section",
                        text = new
                        {
                            type = "mrkdwn",
                            text = "*⚽ Dano Game Result*"
                        }
                    }
                },
                        attachments = new List<object>
                {
                    new
                    {
                        color = "#36a64f",
                        blocks = blocks
                    }
                }
            };

            string bodyParam = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCallSlack(bodyParam, _webhookUrl);
        }

        private async Task<string> GenerateDoubleLeagueTable(DoubleLeagueMatchModel match)
        {
            var leagueData = await _leagueService.GetLeagueById(match.LeagueId);
            var leagueStandings = await _doubleLeagueMatchService.GetDoubleLeagueStandings(match.LeagueId);
            var asciiTable = leagueData.Name + "\n" + GenerateAsciiTable(leagueStandings.ToList());

            return asciiTable;
        }

        private string GenerateAsciiTable(List<DoubleLeagueStandingsQuery> leagueData)
        {
            // Determine the maximum length of the combined team name strings
            int maxLength = leagueData.Max(item => item.TeamName.Length);
            maxLength = maxLength < 15 ? 15 : maxLength; // Ensure a minimum width for the Team column

            // Create a new TextTable with the number of columns
            var table = new TextTable(8, TableBordersStyle.DESIGN_FORMAL, TableVisibleBorders.SURROUND_HEADER_FOOTER_AND_COLUMNS);

            // Set fixed column width ranges
            table.SetColumnWidthRange(0, 2, 2);    // P
            table.SetColumnWidthRange(1, maxLength, maxLength); // Team
            table.SetColumnWidthRange(2, 2, 2);    // MP
            table.SetColumnWidthRange(3, 2, 2);    // MW
            table.SetColumnWidthRange(4, 2, 2);    // ML
            table.SetColumnWidthRange(5, 2, 2);    // GS
            table.SetColumnWidthRange(6, 2, 2);    // GR
            table.SetColumnWidthRange(7, 6, 6);    // Points

            // Add table headers
            table.AddCell("P");
            table.AddCell("Team");
            table.AddCell("MP");
            table.AddCell("MW");
            table.AddCell("ML");
            table.AddCell("GS");
            table.AddCell("GR");
            table.AddCell("Points");

            // Add rows for each league data entry
            foreach (var item in leagueData)
            {
                table.AddCell(item.PositionInLeague.ToString());
                table.AddCell(item.TeamName);
                table.AddCell(item.MatchesPlayed.ToString());
                table.AddCell(item.TotalMatchesWon.ToString());
                table.AddCell(item.TotalMatchesLost.ToString());
                table.AddCell(item.TotalGoalsScored.ToString());
                table.AddCell(item.TotalGoalsRecieved.ToString());
                table.AddCell(item.Points.ToString());
            }

            // Render the table to a string and return it
            return table.Render();
        }

        public async Task SendSlackMessageForDoubleLeague(DoubleLeagueMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.SlackWebhookUrl))
                {
                    _webhookUrl = data.SlackWebhookUrl;
                }
            }
            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new Exception("Slack webhook URL not found for the organisation.");
            }

            var message = await GenerateDoubleLeagueMessage(match);
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCallSlack(jsonPayload, _webhookUrl);
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

        private async Task<object> GenerateDoubleLeagueMessage(DoubleLeagueMatchModel match)
        {
            var teamOne =  _doubleLeagueTeamService.GetDoubleLeagueTeamById(match.TeamOneId);
            var teamTwo = _doubleLeagueTeamService.GetDoubleLeagueTeamById(match.TeamTwoId);

            bool isTeamOneWinner = match.TeamOneScore > match.TeamTwoScore;
            var winnerTeam = isTeamOneWinner ? teamOne : teamTwo;
            var loserTeam = isTeamOneWinner ? teamTwo : teamOne;
            int winnerScore = isTeamOneWinner ? match.TeamOneScore.GetValueOrDefault() : match.TeamTwoScore.GetValueOrDefault();
            int loserScore = isTeamOneWinner ? match.TeamTwoScore.GetValueOrDefault() : match.TeamOneScore.GetValueOrDefault();

            TimeSpan matchDuration = (match.EndTime.HasValue && match.StartTime.HasValue)
                ? match.EndTime.Value - match.StartTime.Value
                : TimeSpan.Zero;
            string formattedDuration = FormatDuration(matchDuration);
          
            // Get league standings.
            var leagueStandings = GenerateDoubleLeagueStandings(match);

            var message = new
            {
                attachments = new[]
                {
            new
            {
                color = "#36a64f",  // Green color for left border
                blocks = new List<object>
                {
                    new
                    {
                        type = "header",
                        text = new
                        {
                            type = "plain_text",
                            text = "⚽ Dano Game Result",
                            emoji = true
                        }
                    },
                    new
                    {
                        type = "section",
                        text = new
                        {
                            type = "mrkdwn",
                            text = $":trophy: *{winnerTeam.Name} wins!*"
                        }
                    },
                    new
                    {
                        type = "section",
                        fields = new List<object>
                        {
                            new { type = "mrkdwn", text = $"*Winner Team:*\n{winnerTeam.Name}\n{await GetTeamMembersString(winnerTeam)}" },
                            new { type = "mrkdwn", text = $"*Loser Team:*\n{loserTeam.Name}\n{await GetTeamMembersString(loserTeam)}" },
                            new { type = "mrkdwn", text = $"*Final Score:*\n{winnerScore} - {loserScore}" },
                            new { type = "mrkdwn", text = $"*Match Duration:*\n{formattedDuration}" },
                            new { type = "mrkdwn", text = $"*League Standings:*\n{leagueStandings}" }
                        }
                    },
                    new
                    {
                        type = "context",
                        elements = new List<object>
                        {
                            new
                            {
                                type = "image",
                                image_url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1",
                                alt_text = "Dano Foosball logo"
                            },
                            new
                            {
                                type = "mrkdwn",
                                text = "Powered by Dano Foosball"
                            }
                        }
                    }
                }
            }
        }
            };

            return message;
        }

        private async Task<string> GetTeamMembersString(DoubleLeagueTeamModel team)
        {
            string result = "";
            var data = await _doubleLeagueMatchService.GetTeamMembers(team.Id);

            foreach (var item in data)
            {
                result += $"{item.FirstName} {item.LastName} \n";
            }
            return result;
        }
    }
}

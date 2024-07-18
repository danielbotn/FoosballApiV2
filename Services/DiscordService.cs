﻿using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Models.Matches;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using FoosballApi.Models.Other;
using TextTableFormatter;

namespace FoosballApi.Services
{
    public interface IDiscordService
    {
        Task SendDiscordMessageForFreehandGame(FreehandMatchModel match, int userId);
        Task SendDiscordMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId);
        Task SendDiscordMessageForSingleLeague(SingleLeagueMatchModel match, int userId);
    }

    public class DiscordService : IDiscordService
    {
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        private readonly ILeagueService _leagueService;
        private readonly IDoubleLeagueTeamService _doubleLeagueTeamService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;


        public DiscordService(
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

        private async static Task<string> GetAIMessage(FreehandMatchModel match, User userOne, User userTwo)
        {
            string result = "";
            string userPrompt = $"{userOne.FirstName} ${userOne.LastName} and ${userTwo.FirstName} ${userTwo.LastName} played a foosball match. " +
                $"${userOne.FirstName}  ${userOne.LastName} scored ${match.PlayerOneScore} goals and " +
                $"${userTwo.FirstName} ${userTwo.LastName} scored ${match.PlayerTwoScore} goals. " +
                $"Write a newspaper paragraph for the match. I only want one paragraph. Don't give me options or anything other then the paragraph. Be dramatic. Write like this is one of the bigest sport event in history";
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

        public async Task SendDiscordMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.DiscordWebhookUrl))
                {
                    _webhookUrl = data.DiscordWebhookUrl;
                }
            }

            User playerOneTeamA = await _userService.GetUserById(match.PlayerOneTeamA);
            User playerTwoTeamA = match.PlayerTwoTeamA.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamA.Value) : null;
            User playerOneTeamB = await _userService.GetUserById(match.PlayerOneTeamB);
            User playerTwoTeamB = match.PlayerTwoTeamB.HasValue ? await _userService.GetUserById(match.PlayerTwoTeamB.Value) : null;

            string winnerTeam;
            string loserTeam;
            int winnerScore;
            int loserScore;

            if (match.TeamAScore > match.TeamBScore)
            {
                winnerTeam = $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}" +
                    $"{(playerTwoTeamA != null ? " & " + playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "")}";
                loserTeam = $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}" +
                    $"{(playerTwoTeamB != null ? " & " + playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "")}";
                winnerScore = match.TeamAScore.GetValueOrDefault();
                loserScore = match.TeamBScore.GetValueOrDefault();
            }
            else
            {
                winnerTeam = $"{playerOneTeamB.FirstName} {playerOneTeamB.LastName}" +
                    $"{(playerTwoTeamB != null ? " & " + playerTwoTeamB.FirstName + " " + playerTwoTeamB.LastName : "")}";
                loserTeam = $"{playerOneTeamA.FirstName} {playerOneTeamA.LastName}" +
                    $"{(playerTwoTeamA != null ? " & " + playerTwoTeamA.FirstName + " " + playerTwoTeamA.LastName : "")}";
                winnerScore = match.TeamBScore.GetValueOrDefault();
                loserScore = match.TeamAScore.GetValueOrDefault();
            }

            TimeSpan matchDuration = TimeSpan.Zero;
            if (match.StartTime.HasValue && match.EndTime.HasValue)
            {
                matchDuration = match.EndTime.Value - match.StartTime.Value;
            }

            string formattedDuration;
            if (matchDuration.TotalMinutes < 1)
            {
                formattedDuration = $"{matchDuration.Seconds} seconds";
            }
            else if (matchDuration.TotalHours < 1)
            {
                formattedDuration = $"{(int)matchDuration.TotalMinutes} minutes";
            }
            else
            {
                formattedDuration = $"{(int)matchDuration.TotalHours} hours and {(int)matchDuration.Minutes} minutes";
            }

            var content = new
            {
                content = $"**Dano Game Result:**\n\n" +
              $"**Winner Team:** __{winnerTeam}__\n" +
              $"**Loser Team:** __{loserTeam}__\n" +
              $"**Final Score:** {winnerScore} - {loserScore}\n" +
              $"**Match Duration:** {formattedDuration}"
            };

            string bodyParam = System.Text.Json.JsonSerializer.Serialize(content);
            await httpCaller.MakeApiCallSlack(bodyParam, _webhookUrl);
        }

        public async Task SendDiscordMessageForFreehandGame(FreehandMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());
                if (!string.IsNullOrEmpty(data.DiscordWebhookUrl))
                {
                    _webhookUrl = data.DiscordWebhookUrl;
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
            var fields = new List<object>
            {
                new { name = "Winner", value = $"{winner.FirstName} {winner.LastName}", inline = true },
                new { name = "Loser", value = $"{loser.FirstName} {loser.LastName}", inline = true },
                new { name = "Final Score", value = $"{winnerScore} - {loserScore}", inline = false },
                new { name = "Match Duration", value = formattedDuration, inline = true },
            };
            var embed = new
            {
                title = "⚽ Dano Game Result",
                color = 3447003,  // Discord blue color
                author = new
                {
                    name = $"{winner.FirstName} {winner.LastName} wins!",
                    icon_url = winner.PhotoUrl
                },
                thumbnail = new
                {
                    url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1"
                },
                fields,
                footer = new
                {
                    text = "Powered by Dano Foosball",
                    icon_url = "https://gcdnb.pbrd.co/images/TtmuzZBe5imH.png?o=1"
                },
                timestamp = match.EndTime ?? DateTime.UtcNow
            };
            var content = new
            {
                embeds = new[] { embed }
            };
            string bodyParam = System.Text.Json.JsonSerializer.Serialize(content);
            await httpCaller.MakeApiCallSlack(bodyParam, _webhookUrl);
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds} seconds";
            if (duration.TotalHours < 1)
                return $"{(int)duration.TotalMinutes} minutes";
            return $"{(int)duration.TotalHours} hours and {duration.Minutes} minutes";
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

        private async Task<string> GenerateSingleLeagueTable(SingleLeagueMatchModel match)
        {
            var leagueData = await _leagueService.GetLeagueById(match.LeagueId);
            var leagueStandings = await _singleLeagueMatchService.GetSigleLeagueStandings(match.LeagueId);
            var ascciiTable = leagueData.Name + "\n" + GenerateAsciiTable(leagueStandings.ToList());

            return ascciiTable;
        }

        private async Task<object> GenerateSingleLeagueMessage(SingleLeagueMatchModel match)
        {
            User playerOne = await _userService.GetUserById(match.PlayerOne);
            User playerTwo = await _userService.GetUserById(match.PlayerTwo);
            string winnerName;
            string loserName;
            int winnerScore;
            int loserScore;
            if (match.PlayerOneScore > match.PlayerTwoScore)
            {
                winnerName = $"{playerOne.FirstName} {playerOne.LastName}";
                loserName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                winnerScore = match.PlayerOneScore.GetValueOrDefault();
                loserScore = match.PlayerTwoScore.GetValueOrDefault();
            }
            else
            {
                winnerName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                loserName = $"{playerOne.FirstName} {playerOne.LastName}";
                winnerScore = match.PlayerTwoScore.GetValueOrDefault();
                loserScore = match.PlayerOneScore.GetValueOrDefault();
            }

            TimeSpan matchDuration = (match.EndTime.HasValue && match.StartTime.HasValue)
                        ? match.EndTime.Value - match.StartTime.Value
                        : TimeSpan.Zero;
            string formattedDuration;
            if (matchDuration.TotalMinutes < 1)
            {
                formattedDuration = $"{matchDuration.Seconds} seconds";
            }
            else if (matchDuration.TotalHours < 1)
            {
                formattedDuration = $"{(int)matchDuration.TotalMinutes} minutes";
            }
            else
            {
                formattedDuration = $"{(int)matchDuration.TotalHours} hours and {(int)matchDuration.Minutes} minutes";
            }

            var leagueStandings = await _singleLeagueMatchService.GetSigleLeagueStandings(match.LeagueId);
            var asciiTable = GenerateAsciiTable(leagueStandings.ToList());

            var content = new
            {
                content = $"**Dano Game Result:**\n\n" +
                          $"**Winner:** **{winnerName}**\n" +
                          $"**Loser:** **{loserName}**\n" +
                          $"**Final Score:** {winnerScore} - {loserScore}\n" +
                          $"**Match Duration:** {formattedDuration}\n\n" +
                          $"**League Standings:**\n```\n{asciiTable}\n```"
            };

            return content;
        }

        public async Task SendDiscordMessageForSingleLeague(SingleLeagueMatchModel match, int userId)
        {
            HttpCaller httpCaller = new();
            string _webhookUrl = "";
            User player = await _userService.GetUserById(userId);
            if (player != null && player.CurrentOrganisationId != null)
            {
                OrganisationModel data = await _organisationService.GetOrganisationById(player.CurrentOrganisationId.GetValueOrDefault());

                if (!string.IsNullOrEmpty(data.DiscordWebhookUrl))
                {
                    _webhookUrl = data.DiscordWebhookUrl;
                }
            }

            if (string.IsNullOrEmpty(_webhookUrl))
            {
                throw new Exception("Slack webhook URL not found for the organisation.");
            }

            var messageGeneral = await GenerateSingleLeagueMessage(match);
            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(messageGeneral);
            await httpCaller.MakeApiCallSlack(jsonPayload, _webhookUrl);
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
    }
}
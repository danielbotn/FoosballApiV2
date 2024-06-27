using FoosballApi.Helpers;
using FoosballApi.Models.Matches;
using FoosballApi.Models;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using FoosballApi.Models.Other;
using TextTableFormatter;

namespace FoosballApi.Services
{
    public interface ISlackService
    {
        Task SendSlackMessageForSingleLeague(SingleLeagueMatchModel match, int userId);
        Task SendSlackMessageForFreehandGame(FreehandMatchModel match, int userId);
        Task SendSlackMessageForFreehandDoubleGame(FreehandDoubleMatchModel match, int userId);
    }

    public class SlackService : ISlackService
    {
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IUserService _userService;
        private readonly IOrganisationService _organisationService;
        private readonly ILeagueService _leagueService;
        

        public SlackService(
            ISingleLeagueMatchService singleLeagueMatchService,
            IUserService userService,
            IOrganisationService organisationService,
            ILeagueService leagueService
           )
        {
            _singleLeagueMatchService = singleLeagueMatchService;
            _userService = userService;
            _organisationService = organisationService;
            _leagueService = leagueService;


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


            var messageTable = new
            {
                text = "```\n" + await GenerateSingleLeagueTable(match) +  "\n```"
            };

            var messageGeneral = await GenerateSingleLeagueMessage(match);

            string jsonPayload= System.Text.Json.JsonSerializer.Serialize(messageGeneral);
            string jsonPayloadTwo = System.Text.Json.JsonSerializer.Serialize(messageTable);
            await httpCaller.MakeApiCallSlack(jsonPayload, _webhookUrl);
            await httpCaller.MakeApiCallSlack(jsonPayloadTwo, _webhookUrl);
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

            var ascciiTable = GenerateAsciiTable(leagueStandings.ToList());

           
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
                            text = $"*Dano Game Results:*\n\n" +
                                   //$"{await GetAIMessage(match, playerOne, playerTwo)}  \n\n" +
                                   $"*Winner:*\n{winnerName}\n" +
                                   $"*Loser:*\n{loserName}\n" +
                                   $"*Final Score:*\n{winnerScore} - {loserScore}\n" +
                                   $"*Match Duration:*\n{formattedDuration}\n\n"
                        }
                    },
                }
            };

            return message;
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

            string winnerName;
            string loserName;
            int winnerScore;
            int loserScore;

            if (match.PlayerOneScore > match.PlayerTwoScore)
            {
                winnerName = $"{playerOne.FirstName} {playerOne.LastName}";
                loserName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                winnerScore = match.PlayerOneScore;
                loserScore = match.PlayerTwoScore;
            }
            else
            {
                winnerName = $"{playerTwo.FirstName} {playerTwo.LastName}";
                loserName = $"{playerOne.FirstName} {playerOne.LastName}";
                winnerScore = match.PlayerTwoScore;
                loserScore = match.PlayerOneScore;
            }

            TimeSpan matchDuration = match.EndTime.HasValue ? match.EndTime.Value - match.StartTime : TimeSpan.Zero;

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

            var message = new
            {
                text = $"Dano Game Results:\n\n" +
                    $"{await GetAIMessage(match, playerOne, playerTwo)}  \n" +
                    "\n" +
                    $"Winner: {winnerName}\n" +
                    $"Loser: {loserName}\n" +
                    $"Final Score: {winnerScore} - {loserScore}\n" +
                    $"Match Duration: {formattedDuration}"
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

            var message = new
            {
                text = $"Dano Game Results:\n\n" +
                    $"{await GetAIMessage(match, playerOneTeamA, playerTwoTeamA, playerOneTeamB, playerTwoTeamB)}\n" +
                    "\n" +
                    $"Winner Team: {winnerTeam}\n" +
                    $"Loser Team: {loserTeam}\n" +
                    $"Final Score: {winnerScore} - {loserScore}\n" +
                    $"Match Duration: {formattedDuration}"
            };

            string bodyParam = System.Text.Json.JsonSerializer.Serialize(message);
            await httpCaller.MakeApiCallSlack(bodyParam, _webhookUrl);
        }
    }
}

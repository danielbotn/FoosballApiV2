using FoosballApi.Helpers;
using FoosballApi.Models.Cms;
using Newtonsoft.Json;

namespace FoosballApi.Services
{
    public interface ICmsService
    {
        Task<HardcodedStrings> GetHardcodedStrings(string language);
    }
    public class CmsService : ICmsService
    {
        public async Task<HardcodedStrings> GetHardcodedStrings(string language)
        {
            HttpCaller httpCaller = new();
            string query = "{ hardcodedString(locale: "
                + language
                + ") {matches newGame quickActions lastTenMatches newGame statistics history leagues pricing settings about logout language darkTheme lightTheme changePassword enableNotifications common security selectLanguage english islenska svenska won lost scored recieved goals username personalinformation user organisation integration slack discord matchDetails totalPlayingTime newMatch rematch twoPlayers fourPlayers choosePlayers match startGame chooseTeammate chooseOpponent chooseOpponents matchReport game resume pause close yes no cancel areYouSureAlert currentOrganisation organisations players newOrganisation addPlayers nameOfNewOrganisation create newOrganisationSuccessMessage newOrganisationErrorMessage organisationSettings createNewOrganisation joinExistingOrganisation managePlayers changeOrganisation information organisationCode letOtherPlayersJoinYourOrganisation joinOrganisation scanQrCode success failure obsFailure cameraPermissionWasDenied unknownError youPressedTheBackButton joinOrganisationInfo joinOrganisationInfo2 actions joinExistingOrganisationWithQrCode organisationCardInfo managePlayer admin deleteUser active inactive changeOrganisationAlertText deleteThisMatch deleteMatchAreYouSure createGroupPlayer groupPlayerInfoText groupPlayerCreateFailure groupPlayerCreateSuccess firstName lastName league createNewLeague createLeague leagueName standings fixtures notStarted welcomeTextBody welcomeTextButton welcomeTextHeadline noUsersExists noData noOrganisation pleaseCheckYourInbox passwordSuccessfullyChanged enterNewPassword newPassword pleaseEnterVerificationCode submitPasswordButtonText submitVerificationButtonText enterSlackWebhook slackWebhook save createTeam teamName errorCouldNotCreateTeam addTeam selectedPlayers startLeague totalTeamsInLeague partOfLeagueToStartIt startLeagueError enterDiscordWebhook discordWebhook discordWebhookError discordWebhookUpdated enterTeamsWebhook teamsWebhook teamsWebhookError teamsWebhookUpdated slackWebhookError slackWebhookUpdated finished ongoing doubleLeague singleLeague } }";
            var iCmsBody = new ICmsBody
            {
                query = query
            };
            string URL = "https://graphql.datocms.com/";
            string bodyParam = System.Text.Json.JsonSerializer.Serialize(iCmsBody);
            string data = await httpCaller.MakeApiCall(bodyParam, URL);
            var resultToJson = JsonConvert.DeserializeObject<Root>(data);
            var hardcodedStrings = resultToJson.data.hardcodedString;
            return hardcodedStrings;
        }
    }
}
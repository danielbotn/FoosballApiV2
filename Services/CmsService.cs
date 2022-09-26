using System.Threading.Tasks;
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
                + ") {matches newGame quickActions lastTenMatches newGame statistics history leagues pricing settings about logout language darkTheme lightTheme changePassword enableNotifications common security selectLanguage english islenska svenska won lost scored recieved goals username personalinformation user organisation integration slack discord matchDetails totalPlayingTime newMatch rematch twoPlayers fourPlayers choosePlayers match startGame chooseTeammate chooseOpponent chooseOpponents matchReport game resume pause close yes no cancel areYouSureAlert currentOrganisation organisations players newOrganisation addPlayers nameOfNewOrganisation create newOrganisationSuccessMessage newOrganisationErrorMessage } }";

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
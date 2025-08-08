using System.Text.Json.Serialization;
namespace FoosballApi.Models.Google;
public class GoogleUserInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }
    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }
    public string Picture { get; set; }
}
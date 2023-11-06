namespace FoosballApi.Dtos.DoubleLeaguePlayers
{
    public class DoubleLeaguePlayerCreateDto
    {
        public int UserOneId { get; set; }
        public int UserTwoId { get; set; }
        public int TeamId { get; set; }
    }
}
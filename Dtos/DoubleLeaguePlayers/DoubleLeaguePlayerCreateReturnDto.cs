namespace FoosballApi.Dtos.DoubleLeaguePlayers
{
    public class DoubleLeaguePlayerCreateReturnDto
    {
        public int PlayerOneId { get; set; }
        public int PlayerTwoId { get; set; }
        public bool InsertionSuccessfull { get; set;}
    }
}
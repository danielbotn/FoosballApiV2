using FoosballApi.Dtos.Users;

namespace FoosballApi.Dtos.SingleLeaguePlayers
{
    public class SingleLeaguePlayersInsertDto
    {
        public List<UserReadDto> Users { get; set; }
        public int LeagueId { get; set; }
    }
}
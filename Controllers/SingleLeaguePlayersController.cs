using FoosballApi.Dtos.SingleLeaguePlayers;
using FoosballApi.Dtos.Users;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SingleLeaguePlayersController : ControllerBase
    {
        private readonly ISingleLeaguePlayersService _singleLeaguePlayersService;
        private readonly ILeagueService _leagueService;

        public SingleLeaguePlayersController(ISingleLeaguePlayersService singleLeaguePlayersService, ILeagueService leagueService)
        {
            _singleLeaguePlayersService = singleLeaguePlayersService;
            _leagueService = leagueService;
        }

        [HttpPost("")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddSingleLeaguePlayers([FromBody] SingleLeaguePlayersInsertDto singleLeaguePlayersInsertDto)
        {
            try 
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), int.Parse(currentOrganisationId));
                
                if (!hasAccess)
                    return Forbid();

                await _singleLeaguePlayersService.AddSingleLeaguePlayers(singleLeaguePlayersInsertDto.Users, singleLeaguePlayersInsertDto.LeagueId);
                await _singleLeaguePlayersService.StartLeague(singleLeaguePlayersInsertDto.LeagueId);

                return Ok();
            }
            catch(Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
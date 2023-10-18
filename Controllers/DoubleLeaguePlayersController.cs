using AutoMapper;
using FoosballApi.Dtos.DoubleLeaguePlayers;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoubleLeaguePlayersController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IDoubleLeaguePlayerService _doubleLeaugePlayerService;
        private readonly IDoubleLeagueTeamService _doubleLeagueTeamService;

        public DoubleLeaguePlayersController(IMapper mapper, 
            IDoubleLeaguePlayerService doubleLeaugePlayerService, 
            IDoubleLeagueTeamService doubleLeagueTeamService)
        {
            _mapper = mapper;
            _doubleLeaugePlayerService = doubleLeaugePlayerService;
            _doubleLeagueTeamService = doubleLeagueTeamService;
        }

        [HttpGet("{leagueId}")]
        [ProducesResponseType(typeof(List<DoubleLeaguePlayerReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDoubleLeaguePlayersByLeagueId(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                bool permission = await _doubleLeagueTeamService.CheckLeaguePermissionEasy(leagueId, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid();

                var allPlayers = await _doubleLeaugePlayerService.GetDoubleLeaguePlayersyLeagueId(leagueId);

                if (allPlayers == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<DoubleLeaguePlayerReadDto>>(allPlayers));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("player/{id}", Name = "GetDoubleLeaguePlayerById")]
        [ProducesResponseType(typeof(DoubleLeaguePlayerReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDoubleLeaguePlayerById(int id)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeagueTeamService.CheckDoubleLeagueTeamPermission(id, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid();

                var playerData = await _doubleLeaugePlayerService.GetDoubleLeaguePlayerById(id);

                return Ok(_mapper.Map<DoubleLeaguePlayerReadDto>(playerData));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
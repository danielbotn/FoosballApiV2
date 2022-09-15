using AutoMapper;
using FoosballApi.Dtos.SingleLeagueMatches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SingleLeagueMatchesController : ControllerBase
    {
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IMapper _mapper;

        public SingleLeagueMatchesController(ISingleLeagueMatchService singleLeagueMatchService, IMapper mapper)
        {
            _mapper = mapper;
            _singleLeagueMatchService = singleLeagueMatchService;
        }

        [HttpGet()]
        [ProducesResponseType(typeof(IEnumerable<SingleLeagueMatchReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllSingleLeaguesMatchesByOrganisationId(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _singleLeagueMatchService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var allMatches = await _singleLeagueMatchService.GetAllMatchesByOrganisationId(int.Parse(currentOrganisationId), leagueId);

                return Ok(allMatches);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("{matchId}", Name = "GetSingleLeagueMatchById")]
        [ProducesResponseType(typeof(IEnumerable<SingleLeagueMatchReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<SingleLeagueMatchReadDto>> GetSingleLeagueMatchById(int matchId)
        {
            try
            {
                string userId = User.Identity.Name;

                bool hasPermission = await _singleLeagueMatchService.CheckMatchPermission(matchId, int.Parse(userId));

                if (!hasPermission)
                    return Forbid();

                var match = await _singleLeagueMatchService.GetSingleLeagueMatchByIdExtended(matchId);

                if (match == null)
                    return NotFound();

                return Ok(_mapper.Map<SingleLeagueMatchReadDto>(match));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
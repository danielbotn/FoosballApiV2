using AutoMapper;
using FoosballApi.Dtos.Leagues;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaguesController : ControllerBase
    {
        private readonly ILeagueService _leagueService;
        // private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;
        private readonly IMapper _mapper;

        public LeaguesController(
            ILeagueService leagueService,
            //ISingleLeagueMatchService singleLeagueMatchService,
            IMapper mapper,
            IDoubleLeaugeMatchService doubleLeagueMatchService)
        {
            _leagueService = leagueService;
           // _singleLeagueMatchService = singleLeagueMatchService;
            _doubleLeagueMatchService = doubleLeagueMatchService;
            _mapper = mapper;
        }

        [HttpGet("organisation")]
        [ProducesResponseType(typeof(List<LeagueReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LeagueReadDto>>> GetLeaguesByOrganisation(int organisationId)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), organisationId);
                if (!hasAccess)
                    return Forbid();

                var leagues = await _leagueService.GetLeaguesByOrganisationId(organisationId);

                if (leagues == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<LeagueReadDto>>(leagues));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
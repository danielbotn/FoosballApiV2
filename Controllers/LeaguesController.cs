using AutoMapper;
using FoosballApi.Dtos.Leagues;
using FoosballApi.Models.Leagues;
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

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(LeagueReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<LeagueReadDto>> GetLeagueById()
        {
            try
            {
                string leagueId = RouteData.Values["id"].ToString();
                string userId = User.Identity.Name;

                int organisationId = await _leagueService.GetOrganisationId(int.Parse(leagueId));

                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), organisationId);

                if (!hasAccess)
                    return Forbid();

                LeagueModel leagueModel = await _leagueService.GetLeagueById(int.Parse(leagueId));

                if (leagueModel == null)
                    return NotFound();

                return Ok(_mapper.Map<LeagueReadDto>(leagueModel));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
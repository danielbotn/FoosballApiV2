using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueMatches;
using FoosballApi.Dtos.Leagues;
using FoosballApi.Dtos.SingleLeagueMatches;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Leagues;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LeaguesController : ControllerBase
    {
        private readonly ILeagueService _leagueService;
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IDoubleLeaugeMatchService _doubleLeagueMatchService;
        private readonly IMapper _mapper;

        public LeaguesController(
            ILeagueService leagueService,
            ISingleLeagueMatchService singleLeagueMatchService,
            IMapper mapper,
            IDoubleLeaugeMatchService doubleLeagueMatchService)
        {
            _leagueService = leagueService;
            _singleLeagueMatchService = singleLeagueMatchService;
            _doubleLeagueMatchService = doubleLeagueMatchService;
            _mapper = mapper;
        }

        [HttpGet("organisation/{organisationId}")]
        [ProducesResponseType(typeof(List<LeagueModel>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LeagueModel>>> GetLeaguesByOrganisation(int organisationId)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), organisationId);
                if (!hasAccess)
                    return Forbid();

                var leagues = await _leagueService.GetLeaguesByOrganisationId(organisationId, int.Parse(userId));

                if (leagues == null)
                    return NotFound();

                return Ok(leagues);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("{id}", Name = "GetLeagueById")]
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

        [HttpPatch("{leagueId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PartialLeagueUpdate(int leagueId, JsonPatchDocument<LeagueUpdateDto> patchDoc)
        {
            try
            {
                var leagueItem = await _leagueService.GetLeagueById(leagueId);

                if (leagueItem == null)
                    return NotFound();

                string userId = User.Identity.Name;

                int organisationId = await _leagueService.GetOrganisationId(leagueId);

                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), organisationId);

                if (!hasAccess)
                    return Forbid();

                var leagueToPatch = _mapper.Map<LeagueUpdateDto>(leagueItem);
                patchDoc.ApplyTo(leagueToPatch, ModelState);

                if (!TryValidateModel(leagueToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(leagueToPatch, leagueItem);

                _leagueService.UpdateLeague(leagueItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("league-players")]
        [ProducesResponseType(typeof(List<LeaguePlayersReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<LeaguePlayersReadDto>>> GetLeaguePlayers(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                int organisationId = await _leagueService.GetOrganisationId(leagueId);

                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), organisationId);

                if (!hasAccess)
                    return Forbid();

                var leaguePlayers = await _leagueService.GetLeaguesPlayers(leagueId);

                if (leaguePlayers == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<LeaguePlayersReadDto>>(leaguePlayers));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost()]
        [ProducesResponseType(typeof(LeagueModel), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateLeague([FromBody] LeagueModelCreate leagueModelCreate)
        {
            try
            {
                int userId = int.Parse(User.Identity.Name);
                bool hasAccess = await _leagueService.CheckLeagueAccess(userId, leagueModelCreate.OrganisationId);

                if (!hasAccess)
                    return Forbid();

                LeagueModel newLeague = await _leagueService.CreateLeague(leagueModelCreate);
                
                return CreatedAtRoute("GetLeagueById", new { id = newLeague.Id }, newLeague);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{leagueId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteLeagueById(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                LeagueModel league = await _leagueService.GetLeagueById(leagueId);
                if (league == null)
                    return NotFound();

                bool hasAccess = await _leagueService.CheckLeagueAccess(int.Parse(userId), league.OrganisationId);

                if (!hasAccess)
                    return Forbid();

                _leagueService.DeleteLeague(league);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("single-league/standings")]
        [ProducesResponseType(typeof(List<SingleLeagueStandingsReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SingleLeagueStandingsReadDto>>> GetSingleLeagueStandings(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                bool permission = await _singleLeagueMatchService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var standings = await _singleLeagueMatchService.GetSigleLeagueStandings(leagueId);

                return Ok(_mapper.Map<IEnumerable<SingleLeagueStandingsReadDto>>(standings));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("double-league/standings")]
        [ProducesResponseType(typeof(List<SingleLeagueStandingsReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<DoubleLeagueStandingsReadDto>>> GetDoubleLeagueStandings(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                bool permission = await _doubleLeagueMatchService.CheckLeaguePermissionByOrganisationId(leagueId, int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid(); // hér þarf að athuga

                var standings = await _doubleLeagueMatchService.GetDoubleLeagueStandings(leagueId);

                return Ok(_mapper.Map<IEnumerable<DoubleLeagueStandingsReadDto>>(standings));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
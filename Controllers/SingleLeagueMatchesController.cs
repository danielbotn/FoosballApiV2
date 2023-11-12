using AutoMapper;
using FoosballApi.Dtos.SingleLeagueMatches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
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
        public async Task<ActionResult> GetAllSingleLeagueMatchesByLeagueId(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _singleLeagueMatchService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var allMatches = await _singleLeagueMatchService.GetAllMatchesByLeagueId(leagueId);

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

        [HttpPatch("")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateSingleLeagueMatch(int matchId, JsonPatchDocument<SingleLeagueMatchUpdateDto> patchDoc)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasPermission = await _singleLeagueMatchService.CheckMatchPermission(matchId, int.Parse(userId));

                if (!hasPermission)
                    return Forbid();

                var match = await _singleLeagueMatchService.GetSingleLeagueMatchById(matchId);

                if (match == null)
                    return NotFound();

                var matchToPatch = _mapper.Map<SingleLeagueMatchUpdateDto>(match);
                patchDoc.ApplyTo(matchToPatch, ModelState);

                if (!TryValidateModel(matchToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(matchToPatch, match);

                _singleLeagueMatchService.UpdateSingleLeagueMatch(match);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("reset-match")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> ResetSingleLeagueMatchById(int matchId)
        {
            try
            {
                string userId = User.Identity.Name;

                var matchItem = await _singleLeagueMatchService.GetSingleLeagueMatchById(matchId);
                
                if (matchItem == null)
                    return NotFound();

                bool hasPermission = await _singleLeagueMatchService.CheckMatchPermission(matchId, int.Parse(userId));

                if (!hasPermission)
                    return Forbid();

                _singleLeagueMatchService.ResetMatch(matchItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("create-matches")]
        public async Task<ActionResult> CreateSingleLeagueMatches([FromBody] CreateSingleLeagueMatchesBody body)
        {
            try
            {
                string userId = User.Identity.Name;

                bool permission = await _singleLeagueMatchService.CheckLeaguePermission(body.LeagueId, int.Parse(userId));

                if (!permission)
                    return Forbid(); // hér þarf að athuga
                
                var matches = await _singleLeagueMatchService.CreateSingleLeagueMatches(body);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

    }   
}
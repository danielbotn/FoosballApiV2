using FoosballApi.Models;
using FoosballApi.Models.Matches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MatchesController: ControllerBase
    {
        private readonly IMatchService _matchService;
        public MatchesController(IMatchService matchService)
        {
            _matchService = matchService;
        }

        [HttpGet("live-matches")]
        [ProducesResponseType(typeof(List<Match>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Match>>> GetLiveMatches()
        {
            try
            {
                var matches = await _matchService.GetLiveMatches();

                if (matches == null)
                    return NotFound();

                return Ok(matches);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
using AutoMapper;
using FoosballApi.Dtos.Goals;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FreehandGoalsController : ControllerBase
    {
        private readonly IFreehandGoalService _goalService;
        private readonly IFreehandMatchService _matchService;
        private readonly IMapper _mapper;

        public FreehandGoalsController(IFreehandGoalService goalService, IFreehandMatchService matchService, IMapper mapper)
        {
            _mapper = mapper;
            _goalService = goalService;
            _matchService = matchService;
        }

        [HttpGet("goals/{matchId}")]
        [ProducesResponseType(typeof(List<FreehandGoalReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FreehandGoalReadDto>>> GetFreehandGoalsByMatchId()
        {
            try
            {
                string matchId = RouteData.Values["matchId"].ToString();
                string userId = User.Identity.Name;
                bool access = await _matchService.CheckFreehandMatchPermission(int.Parse(matchId), int.Parse(userId));

                if (!access)
                    return Forbid();

                var allGoals = await _goalService.GetFreehandGoalsByMatchId(int.Parse(matchId), int.Parse(userId));

                if (allGoals == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<FreehandGoalReadDto>>(allGoals));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
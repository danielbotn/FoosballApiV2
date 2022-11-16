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

        [HttpGet("{goalId}", Name = "GetFreehandGoalById")]
        [ProducesResponseType(typeof(FreehandGoalReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<FreehandGoalReadDto>> GetFreehandGoalById(int matchId)
        {
            try
            {
                string goalId = RouteData.Values["goalId"].ToString();
                string userId = User.Identity.Name;

                bool matchPermission = await _matchService.CheckFreehandMatchPermission(matchId, int.Parse(userId));

                if (!matchPermission)
                    return Forbid();

                bool goalPermission = await _goalService.CheckGoalPermission(int.Parse(userId), matchId, int.Parse(goalId));

                if (!goalPermission)
                    return Forbid();

                var allMatches = await _goalService.GetFreehandGoalById(int.Parse(goalId));

                if (allMatches == null)
                    return NotFound();

                return Ok(_mapper.Map<FreehandGoalReadDto>(allMatches));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(FreehandGoalCreateResultDto), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateFreehandGoal([FromBody] FreehandGoalCreateDto freehandGoalCreateDto)
        {
            try
            {
                int matchId = freehandGoalCreateDto.MatchId;
                string userId = User.Identity.Name;

                bool access = await _matchService.CheckFreehandMatchPermission(matchId, int.Parse(userId));

                if (!access)
                    return Forbid();
                
                if (freehandGoalCreateDto.ScoredByUserId != int.Parse(userId) && freehandGoalCreateDto.OponentId != int.Parse(userId))
                    return Forbid();

                var newGoal = await _goalService.CreateFreehandGoal(int.Parse(userId), freehandGoalCreateDto);

                var freehandGoalReadDto = _mapper.Map<FreehandGoalCreateResultDto>(newGoal);

                return CreatedAtRoute("GetFreehandGoalById", new { goalId = newGoal.Id }, freehandGoalReadDto);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{matchId}/{goalId}/")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteFeehandGoal(string goalId, string matchId)
        {
            try
            {
                string userId = User.Identity.Name;
                var goalItem = await _goalService.GetFreehandGoalByIdFromDatabase(int.Parse(goalId));
                if (goalItem == null)
                    return NotFound();

                bool hasPermission = await _matchService.CheckFreehandMatchPermission(int.Parse(matchId), int.Parse(userId));

                if (!hasPermission)
                    return Forbid();

                await _goalService.DeleteFreehandGoal(goalItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
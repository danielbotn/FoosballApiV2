using AutoMapper;
using FoosballApi.Dtos.SingleLeagueGoals;
using FoosballApi.Models.SingleLeagueGoals;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SingleLeagueGoalsController : ControllerBase
    {
        private readonly ISingleLeagueGoalService _singleLeagueGoalService;
        private readonly ISingleLeagueMatchService _singleLeagueMatchService;
        private readonly IMapper _mapper;

        public SingleLeagueGoalsController(ISingleLeagueGoalService singleLeagueGoalService, ISingleLeagueMatchService singleLeagueMatchService, IMapper mapper)
        {
            _mapper = mapper;
            _singleLeagueGoalService = singleLeagueGoalService;
            _singleLeagueMatchService = singleLeagueMatchService;
        }

        [HttpGet()]
        [ProducesResponseType(typeof(List<SingleLeagueGoalReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SingleLeagueGoalReadDto>>> GetAllSingleLeagueGoalsByMatchId(int leagueId, int matchId)
        {
            try
            {
                string userId = User.Identity.Name;

                bool permission = await _singleLeagueMatchService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var allGoals = await _singleLeagueGoalService.GetAllSingleLeagueGoalsByMatchId(matchId);

                return Ok(_mapper.Map<IEnumerable<SingleLeagueGoalReadDto>>(allGoals));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("{goalId}", Name = "getSingleLeagueById")]
        [ProducesResponseType(typeof(SingleLeagueGoalReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<SingleLeagueGoalReadDto>> GetSingleLeagueGoalById(int goalId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _singleLeagueGoalService.CheckSingleLeagueGoalPermission(int.Parse(userId), goalId, int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid();

                var goal = await _singleLeagueGoalService.GetSingleLeagueGoalById(goalId);

                return Ok(_mapper.Map<SingleLeagueGoalReadDto>(goal));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("")]
        [ProducesResponseType(typeof(SingleLeagueGoalModel), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateSingleLeagueGoal([FromBody] SingleLeagueCreateModel singleLeagueCreateModel)
        {
            try
            {
                string userId = User.Identity.Name;

                bool permission = _singleLeagueGoalService.CheckCreatePermission(int.Parse(userId), singleLeagueCreateModel);

                if (!permission)
                    return Forbid();

                SingleLeagueGoalModel newGoal = await _singleLeagueGoalService.CreateSingleLeagueGoal(singleLeagueCreateModel);

                await _singleLeagueGoalService.UpdateSingleLeagueMatch(newGoal);

                return CreatedAtRoute("getSingleLeagueById", new { goalId = newGoal.Id }, newGoal);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{goalId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteSingleLeagueGoalById(int goalId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                var goalItem = await _singleLeagueGoalService.GetSingleLeagueGoalById(goalId);
                if (goalItem == null)
                    return NotFound();

                bool hasPermission = await _singleLeagueGoalService.CheckSingleLeagueGoalPermission(int.Parse(userId), goalId, int.Parse(currentOrganisationId));

                if (!hasPermission)
                    return Forbid();

                _singleLeagueGoalService.DeleteSingleLeagueGoal(goalItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
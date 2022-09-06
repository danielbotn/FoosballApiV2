using AutoMapper;
using FoosballApi.Dtos.SingleLeagueGoals;
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
    }
}
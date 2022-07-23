using AutoMapper;
using FoosballApi.Dtos.DoubleMatches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FreehandDoubleMatchesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IFreehandDoubleMatchService _doubleMatchService;

        public FreehandDoubleMatchesController(IMapper mapper, IFreehandDoubleMatchService doubleMatchService)
        {
            _mapper = mapper;
            _doubleMatchService = doubleMatchService;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(List<FreehandDoubleMatchReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FreehandDoubleMatchReadDto>>> GetAllFreehandDoubleMatchesByUser()
        {
            try
            {
                string userId = User.Identity.Name;

                var allMatches = await _doubleMatchService.GetAllFreehandDoubleMatches(int.Parse(userId));

                if (allMatches == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<FreehandDoubleMatchReadDto>>(allMatches));
            }
            catch (Exception e)
            {
                return UnprocessableEntity(e);
            }
        }

        [HttpGet("{matchId}", Name = "GetFreehandDoubleMatchByMatchId")]
        [ProducesResponseType(typeof(FreehandDoubleMatchReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<FreehandDoubleMatchReadDto>> GetFreehandDoubleMatchByMatchId()
        {
            try
            {
                string matchId = RouteData.Values["matchId"].ToString();
                string userId = User.Identity.Name;

                bool access = await _doubleMatchService.CheckMatchPermission(int.Parse(userId), int.Parse(matchId));

                if (!access)
                    return Forbid();

                var match = await _doubleMatchService.GetFreehandDoubleMatchByIdExtended(int.Parse(matchId));

                if (match == null)
                    return NotFound();

                return Ok(_mapper.Map<FreehandDoubleMatchReadDto>(match));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
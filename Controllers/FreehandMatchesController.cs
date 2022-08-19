using AutoMapper;
using FoosballApi.Dtos.Matches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FreehandMatchesController : ControllerBase
    {
        private readonly IFreehandMatchService _matchService;
        private readonly IMapper _mapper;

        public FreehandMatchesController(IFreehandMatchService matchService, IMapper mapper)
        {
            _matchService = matchService;
            _mapper = mapper;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(List<FreehandMatchesReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FreehandMatchesReadDto>>> GetAllFreehandMatchesByUser()
        {
            try
            {
                string userId = User.Identity.Name;

                var allMatches = await _matchService.GetAllFreehandMatches(int.Parse(userId));

                if (allMatches == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<FreehandMatchesReadDto>>(allMatches));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
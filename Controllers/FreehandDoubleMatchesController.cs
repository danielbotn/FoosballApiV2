using AutoMapper;
using FoosballApi.Dtos.DoubleMatches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
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

        [HttpPost()]
        [ProducesResponseType(typeof(FreehandDoubleMatchResponseDto), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateDoubleFreehandMatch([FromBody] FreehandDoubleMatchCreateDto freehandDoubleMatchCreateDto)
        {
            string userId = User.Identity.Name;

            if (freehandDoubleMatchCreateDto.PlayerOneTeamA != int.Parse(userId) 
                && freehandDoubleMatchCreateDto.PlayerOneTeamB != int.Parse(userId)
                && freehandDoubleMatchCreateDto.PlayerTwoTeamA != int.Parse(userId)
                && freehandDoubleMatchCreateDto.PlayerTwoTeamB != int.Parse(userId))
            {
                return Forbid();
            }

            var newMatch = await _doubleMatchService.CreateFreehandDoubleMatch(int.Parse(userId), freehandDoubleMatchCreateDto);

            var freehandDoubleMatchReadDto = _mapper.Map<FreehandDoubleMatchResponseDto>(newMatch);

            return CreatedAtRoute("GetFreehandDoubleMatchByMatchId", new { matchId = newMatch.Id }, freehandDoubleMatchReadDto);
        }

        [HttpPatch()]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> UpdateDoubleFreehandMatch(int matchId, JsonPatchDocument<FreehandDoubleMatchUpdateDto> patchDoc)
        {
            try
            {
                string userId = User.Identity.Name;

                var matchItem = await _doubleMatchService.GetFreehandDoubleMatchById(matchId);
                if (matchItem == null)
                    return NotFound();

                bool hasPermission = await _doubleMatchService.CheckMatchPermission(int.Parse(userId), matchId);

                if (!hasPermission)
                    return Forbid();

                var freehandMatchToPatch = _mapper.Map<FreehandDoubleMatchUpdateDto>(matchItem);
                patchDoc.ApplyTo(freehandMatchToPatch, ModelState);

                if (!TryValidateModel(freehandMatchToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(freehandMatchToPatch, matchItem);

                _doubleMatchService.UpdateFreehandMatch(matchItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{matchId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteDoubleFreehandMatch(int matchId)
        {
            try
            {
                string userId = User.Identity.Name;
                var matchItem = await _doubleMatchService.GetFreehandDoubleMatchById(matchId);
                if (matchItem == null)
                    return NotFound();

                bool hasPermission = await _doubleMatchService.CheckMatchPermission(int.Parse(userId), matchId);

                if (!hasPermission)
                    return Forbid();

                _doubleMatchService.DeleteFreehandMatch(matchItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
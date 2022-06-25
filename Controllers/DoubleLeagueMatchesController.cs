using System;
using System.Collections.Generic;
using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueMatches;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoubleLeagueMatchesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IDoubleLeaugeMatchService _doubleLeaugeMatchService;

        public DoubleLeagueMatchesController(IMapper mapper, IDoubleLeaugeMatchService doubleLeaugeMatchService)
        {
            _mapper = mapper;
            _doubleLeaugeMatchService = doubleLeaugeMatchService;
        }

        [HttpGet("")]
        [ProducesResponseType(typeof(List<AllMatchesModelReadDto>), 200)]
        public async Task<ActionResult> GetAllDoubleLeaguesMatchesByLeagueId(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeaugeMatchService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var allMatches = await _doubleLeaugeMatchService.GetAllMatchesByOrganisationId(int.Parse(currentOrganisationId), leagueId);

                return Ok(_mapper.Map<IEnumerable<AllMatchesModelReadDto>>(allMatches));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPatch("")]
        [ProducesResponseType(typeof(NoContentResult), 204)]
        public async Task<ActionResult> UpdateDoubleLeagueMatch(int matchId, JsonPatchDocument<DoubleLeagueMatchUpdateDto> patchDoc)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                var match = await _doubleLeaugeMatchService.GetMatchById(matchId);

                if (match == null)
                    return NotFound();

                bool hasPermission = await _doubleLeaugeMatchService.CheckMatchAccess(matchId, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!hasPermission)
                    return Forbid();

                var matchToPatch = _mapper.Map<DoubleLeagueMatchUpdateDto>(match);
                patchDoc.ApplyTo(matchToPatch, ModelState);

                if (!TryValidateModel(matchToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(matchToPatch, match);

                _doubleLeaugeMatchService.UpdateDoubleLeagueMatch(match);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("reset-match")]
        [ProducesResponseType(typeof(NoContentResult), 204)]
        public async Task<ActionResult> ResetDoubleLeagueMatchById(int matchId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                var matchItem = await _doubleLeaugeMatchService.GetMatchById(matchId);
                if (matchItem == null)
                    return NotFound();

                bool hasPermission = await _doubleLeaugeMatchService.CheckMatchAccess(matchId, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!hasPermission)
                    return Forbid();

                await _doubleLeaugeMatchService.ResetMatch(matchItem, matchId);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("match/{matchId}")]
        [ProducesResponseType(typeof(AllMatchesModelReadDto), 200)]
        public async Task<ActionResult<AllMatchesModelReadDto>> GetDoubleLeagueMatchById(int matchId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeaugeMatchService.CheckMatchAccess(matchId, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid();

                var matchData = await _doubleLeaugeMatchService.GetMatchById(matchId);

                return Ok(_mapper.Map<AllMatchesModelReadDto>(matchData));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

    }
}
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

    }
}
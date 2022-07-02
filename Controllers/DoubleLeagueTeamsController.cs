using System;
using System.Collections.Generic;
using AutoMapper;
using FoosballApi.Dtos.DoubleLeagueTeams;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DoubleLeagueTeamsController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IDoubleLeagueTeamService _doubleLeagueTeamService;

        public DoubleLeagueTeamsController(IMapper mapper, IDoubleLeagueTeamService doubleLeagueTeamService)
        {
            _mapper = mapper;
            _doubleLeagueTeamService = doubleLeagueTeamService;
        }

        [HttpGet("{leagueId}")]
        [ProducesResponseType(typeof(List<DoubleLeagueTeamReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDoubleLeagueTeamsByLeagueId(int leagueId)
        {
            try
            {
                string userId = User.Identity.Name;

                bool permission = await _doubleLeagueTeamService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var allTeams = await _doubleLeagueTeamService.GetDoubleLeagueTeamsByLeagueId(leagueId);

                if (allTeams == null)
                    return NotFound();

                return Ok(_mapper.Map<IEnumerable<DoubleLeagueTeamReadDto>>(allTeams));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("team/{id}", Name = "GetDoubleLeagueTeamById")]
        [ProducesResponseType(typeof(DoubleLeagueTeamReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetDoubleLeagueTeamById(int id)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeagueTeamService.CheckDoubleLeagueTeamPermission(id, int.Parse(userId), int.Parse(currentOrganisationId));

                if (!permission)
                    return Forbid();

                var teamData = _doubleLeagueTeamService.GetDoubleLeagueTeamById(id);

                return Ok(_mapper.Map<DoubleLeagueTeamReadDto>(teamData));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost()]
        [ProducesResponseType(typeof(DoubleLeagueTeamReadDto), StatusCodes.Status201Created)]
        public async Task <ActionResult> CreateDoubleLeagueTeam(int leagueId, string name)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeagueTeamService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                var newTeam = _doubleLeagueTeamService.CreateDoubleLeagueTeam(leagueId, int.Parse(currentOrganisationId), name);

                var doubleLeagueTeamReadDto = _mapper.Map<DoubleLeagueTeamReadDto>(newTeam);

                return CreatedAtRoute("GetDoubleLeagueTeamById", new { id = newTeam.Id }, doubleLeagueTeamReadDto);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{leagueId}/{teamId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task <ActionResult> DeleteDoubleLeagueTeam(int leagueId, int teamId)
        {
            try
            {
                string userId = User.Identity.Name;
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;

                bool permission = await _doubleLeagueTeamService.CheckLeaguePermission(leagueId, int.Parse(userId));

                if (!permission)
                    return Forbid();

                _doubleLeagueTeamService.DeleteDoubleLeagueTeam(teamId);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
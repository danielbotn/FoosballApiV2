using AutoMapper;
using FoosballApi.Dtos.Users;
using FoosballApi.Filter;
using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;


namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserReadDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                string currentOrganisationId = User.FindFirst("CurrentOrganisationId").Value;
                var allUsers = await _userService.GetAllUsers(int.Parse(currentOrganisationId));

                return Ok(_mapper.Map<IEnumerable<UserReadDto>>(allUsers));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("{id}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserReadDto), StatusCodes.Status200OK)]
        public ActionResult<UserReadDto> GetUserById(int id)
        {
            try
            {
                var userItem = _userService.GetUserById(id);

                if (userItem == null)
                    return NotFound();

                return Ok(_mapper.Map<UserReadDto>(userItem));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PartialUserUpdate(int id, JsonPatchDocument<UserUpdateDto> patchDoc)
        {
            try
            {
                var userModelFromRepo = await _userService.GetUserById(id);

                if (userModelFromRepo == null)
                    return NotFound();

                string userId = User.Identity.Name;

                if (int.Parse(userId) != id)
                    return Forbid();

                var userToPatch = _mapper.Map<UserUpdateDto>(userModelFromRepo);
                patchDoc.ApplyTo(userToPatch, ModelState);

                if (!TryValidateModel(userToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(userToPatch, userModelFromRepo);

                _userService.UpdateUser(userModelFromRepo);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var userModelFromRepo = await _userService.GetUserById(id);

                if (userModelFromRepo == null)
                    return NotFound();

                string userId = User.Identity.Name;

                if (int.Parse(userId) != id)
                    return Forbid();

                _userService.DeleteUser(userModelFromRepo);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("stats")]
        [ProducesResponseType(typeof(UserStatsReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetUserMatchesStats()
        {
            try
            {
                string userId = User.Identity.Name;

                var data = await _userService.GetUserStats(int.Parse(userId));

                return Ok(_mapper.Map<UserStatsReadDto>(data));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("stats/last-ten-matches")]
        [ProducesResponseType(typeof(IEnumerable<MatchReadDto>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MatchReadDto>> GetLastTenMatches()
        {
            try
            {
                string userId = User.Identity.Name;

                var data = _userService.GetLastTenMatchesByUserId(int.Parse(userId));

                return Ok(_mapper.Map<IEnumerable<MatchReadDto>>(data));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("stats/history")]
        [ProducesResponseType(typeof(IEnumerable<Match>), StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Match>> History([FromQuery] PaginationFilter filter)
        {
            try
            {
                var validFilter = new PaginationFilter(filter.PageNumber, filter.PageSize);
                string userId = User.Identity.Name;

                var data = _userService.GetPagnatedHistory(int.Parse(userId), validFilter.PageNumber, validFilter.PageSize);
                var order = _userService.OrderMatchesByDescending(data);

                return Ok(order);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
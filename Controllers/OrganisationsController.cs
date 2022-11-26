using AutoMapper;
using FoosballApi.Dtos.Organisations;
using FoosballApi.Models.Organisations;
using FoosballApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrganisationsController : ControllerBase
    {
        private readonly IOrganisationService _organisationService;
        private readonly IMapper _mapper;

        public OrganisationsController(IOrganisationService organisationService, IMapper mapper)
        {
            _organisationService = organisationService;
            _mapper = mapper;
        }

        [HttpGet("{id}", Name = "getOrganisationById")]
        [ProducesResponseType(typeof(OrganisationReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<OrganisationReadDto>> GetOrganisationById(int id)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasPermission = await _organisationService.HasUserOrganisationPermission(int.Parse(userId), id);

                if (!hasPermission)
                    return Forbid();

                var organisation = await _organisationService.GetOrganisationById(id);

                if (organisation == null)
                    return NotFound();

                return Ok(_mapper.Map<OrganisationReadDto>(organisation));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(OrganisationModel), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateOrganisation([FromBody] OrganisationModelCreate organisationModelCreate)
        {
            try
            {
                string userId = User.Identity.Name;

                OrganisationModel newOrganisation = await _organisationService.CreateOrganisation(organisationModelCreate, int.Parse(userId));

                return CreatedAtRoute("getOrganisationById", new { Id = newOrganisation.Id }, newOrganisation);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> PartialOrganisationUpdate(int id, JsonPatchDocument<OrganisationUpdateDto> patchDoc)
        {
            try
            {
                var orgItem = await _organisationService.GetOrganisationById(id);

                if (orgItem == null)
                    return NotFound();

                string userId = User.Identity.Name;
                bool hasPermission = await _organisationService.HasUserOrganisationPermission(int.Parse(userId), id);

                if (!hasPermission)
                    return Forbid();

                var organisationToPatch = _mapper.Map<OrganisationUpdateDto>(orgItem);
                patchDoc.ApplyTo(organisationToPatch, ModelState);

                if (!TryValidateModel(organisationToPatch))
                    return ValidationProblem(ModelState);

                _mapper.Map(organisationToPatch, orgItem);

                _organisationService.UpdateOrganisation(orgItem);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> DeleteOrganisation(int id)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasPermission = await _organisationService.HasUserOrganisationPermission(int.Parse(userId), id);

                if (!hasPermission)
                    return Forbid();

                var organisation = await _organisationService.GetOrganisationById(id);

                if (organisation == null)
                    return NotFound();
                

                _organisationService.DeleteOrganisation(organisation);

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("user")]
        [ProducesResponseType(typeof(OrganisationReadDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<OrganisationReadDto>> GetOrganisationsByUser()
        {
            try
            {
                string userId = User.Identity.Name;
                var userItem = await _organisationService.GetOrganisationsByUser(int.Parse(userId));

                return Ok(_mapper.Map<IEnumerable<OrganisationReadDto>>(userItem));
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("join-organisation")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult> JoinOrganisation([FromBody] JoinOrganisationModel joinOrganisationModel)
        {
            try
            {
                string userId = User.Identity.Name;
                bool isAllowed = await _organisationService.JoinOrganisation(joinOrganisationModel, int.Parse(userId));

                if (!isAllowed)
                    return Forbid();

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("update-is-admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateOrganisationListIsAdmin(int organisationId, int userIdToChange, bool isAdmin)
        {
            try
            {
                string userId = User.Identity.Name;
                bool hasPermission = await _organisationService.HasUserOrganisationPermission(int.Parse(userId), organisationId);
                bool hasPermissionUserToChange = await _organisationService.HasUserOrganisationPermission(userIdToChange, organisationId);

                if (!hasPermission || !hasPermissionUserToChange)
                    return Forbid();
                
                bool updateSuccessfull = await _organisationService.UpdateIsAdmin(organisationId, userIdToChange, isAdmin);

                if (!updateSuccessfull)
                {
                    return Forbid();
                }

                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("leave-or-rejoin-organisation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> LeaveOrRejoinOrganisation(int organisationId, int userId, bool isDeleted)
        {
            try
            {
                string CurrentuserId = User.Identity.Name;
                
                var org = await _organisationService.GetOrganisationsByUser(int.Parse(CurrentuserId));

                bool sameOrganisation = false;
                foreach (var item in org)
                {
                    if (item.Id == organisationId)
                    {
                        sameOrganisation = true;
                        break;
                    }
                }

                bool hasPermission = await _organisationService.HasUserOrganisationPermission(userId, organisationId);

                if (!sameOrganisation || !hasPermission)
                {
                    return Forbid();
                }

                bool updateStatement = await _organisationService.LeaveOrRejoinOrganisation(organisationId, userId, isDeleted);

                if (updateStatement)
                {
                    return Ok();
                }

                return StatusCode(500);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        [HttpPut("change-current-organisation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ChangeCurrentOrganisation(int userId, int currentOrganisationId, int newOrganisationId)
        {
            try
            {
                bool hasPermissionCurrent = await _organisationService.HasUserOrganisationPermission(userId, currentOrganisationId);
                bool hasPermissionNew = await _organisationService.HasUserOrganisationPermission(userId, newOrganisationId);

                if (!hasPermissionCurrent || !hasPermissionNew)
                    return Forbid();
                
                bool upadateStatement = await _organisationService.ChangeCurrentOrganisation(userId, currentOrganisationId, newOrganisationId);

                if (!upadateStatement)
                    return StatusCode(500);
                
                return Ok();
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
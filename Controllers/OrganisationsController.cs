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
                var userItem = await _organisationService.GetOrganisationById(id);

                if (userItem == null)
                    return NotFound();

                return Ok(_mapper.Map<OrganisationReadDto>(userItem));
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

                // need to check permissions

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
    }
}
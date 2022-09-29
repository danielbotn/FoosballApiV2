using System.Security.Cryptography;
using Dapper;
using FoosballApi.Dtos.Organisations;
using FoosballApi.Models;
using FoosballApi.Models.Organisations;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IOrganisationService
    {
        Task<OrganisationModel> GetOrganisationById(int id);
        Task<OrganisationModel> CreateOrganisation(OrganisationModelCreate organisation, int userId);
        Task<bool> HasUserOrganisationPermission(int userId, int organisationId);
        void UpdateOrganisation(OrganisationModel organisation);
        void DeleteOrganisation(OrganisationModel organisation);
        Task<IEnumerable<OrganisationModel>> GetOrganisationsByUser(int id);

    }

    public class OrganisationService : IOrganisationService
    {
        private string _connectionString { get; }
        public OrganisationService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task<OrganisationModel> GetOrganisationById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var organisation = await conn.QueryFirstOrDefaultAsync<OrganisationModel>(
                    @"SELECT id as Id, name as Name, created_at as CreatedAt,
                    organisation_type as OrganisationType, organisation_code AS OrganisationCode
                    FROM organisations
                    WHERE id = @id",
                new { id = id });
                return organisation;
            }
        }

        private string CreateOrganisationCode()
        {
            var randomNumber = new byte[40]; // or 32
            string token = "";

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                token = Convert.ToBase64String(randomNumber);
            }
            string firstFiveOfToken = token.Substring(0, 10);
            return firstFiveOfToken;
        }

        private async Task<string> CheckIfOrganisationCodeExists(string organisationCode)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var orgCode = await conn.QueryFirstOrDefaultAsync<string>(
                    @"SELECT organisation_code
                    FROM organisations
                    WHERE organisation_code = @organisation_code",
                new { organisation_code = organisationCode });
                return orgCode;
            }
        }

        public async Task<OrganisationModel> CreateOrganisation(OrganisationModelCreate organisation, int userId)
        {
            OrganisationModel result = new();
            DateTime now = DateTime.Now;
            result.CreatedAt = now;
            result.Name = organisation.Name;
            result.OrganisationCode = CreateOrganisationCode();

            int retry = 0;
            var orgCodeExist = await CheckIfOrganisationCodeExists(result.OrganisationCode);
            while (orgCodeExist != null && retry < 10) 
            {
                retry++;
                result.OrganisationCode = CreateOrganisationCode();
                orgCodeExist = await CheckIfOrganisationCodeExists(result.OrganisationCode);
            }
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var newOrganistionId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO organisations (name, created_at, organisation_type, organisation_code)
                    VALUES (@name, @created_at, @organisation_type, @organisation_code) RETURNING id",
                    new { 
                        name = organisation.Name, 
                        created_at = now, 
                        organisation_type = organisation.OrganisationType,
                        organisation_code = result.OrganisationCode
                    });
               
                result.Id = newOrganistionId;
            }

            await AddPlayerToOrganisation(result.Id, userId);
            
            return result;
        }

        private async Task AddPlayerToOrganisation(int organisationId, int userId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var newOrganistionId = await conn.ExecuteAsync(
                    @"INSERT INTO organisation_list (organisation_id, user_id, is_admin)
                    VALUES (@organisation_id, @user_id, @is_admin)",
                    new { 
                        organisation_id = organisationId,
                        user_id = userId, 
                        is_admin = true
                    });
            }
        }

        public void UpdateOrganisation(OrganisationModel organisation)
        {
            int organisation_type = (int)organisation.OrganisationType;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE organisations
                    SET name = @name, created_at = @created_at, organisation_type = @organisation_type
                    WHERE id = @id",
                new 
                { 
                    name = organisation.Name, 
                    created_at = organisation.CreatedAt,
                    organisation_type = organisation_type,
                    id = organisation.Id
                });
            }
        }

        public void DeleteOrganisation(OrganisationModel organisation)
        {
            if (organisation == null)
            {
                throw new ArgumentNullException(nameof(organisation));
            }

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM organisations WHERE id = @id",
                    new { id = organisation.Id });
            }
        }

        public async Task<bool> HasUserOrganisationPermission(int userId, int organisationId)
        {
            bool result = false;

            List<OrganisationListModel> organisationLists = new();

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<OrganisationListModel>(
                    @"SELECT id as Id, user_id as UserId, organisation_id as Organisationid
                    FROM organisation_list
                    WHERE user_id = @user_id AND organisation_id = @organisation_id",
                new { user_id= userId, organisation_id = organisationId });
                
                organisationLists = query.ToList();
            }

            foreach (var item in organisationLists)
            {
                if (item.UserId == userId && item.OrganisationId == organisationId)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        public async Task<IEnumerable<OrganisationModel>> GetOrganisationsByUser(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var query = await conn.QueryAsync<OrganisationModel>(
                    @"SELECT o.id as Id, o.name as Name, o.created_at as CreatedAt,
                    o.organisation_type as OrganistionType, o.organisation_code AS OrganisationCode
                    FROM organisations o
                    JOIN organisation_list ol ON ol.organisation_id = o.id
                    WHERE ol.user_id = @user_id",
                new { user_id= id });
                
                return query;
            }
        }
    }
}
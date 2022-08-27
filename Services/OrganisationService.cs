using Dapper;
using FoosballApi.Dtos.Organisations;
using FoosballApi.Models.Organisations;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IOrganisationService
    {
        Task<OrganisationModel> GetOrganisationById(int id);
        Task<OrganisationModel> CreateOrganisation(OrganisationModelCreate organisation, int userId);
        void UpdateOrganisation(OrganisationModel organisation);
        void DeleteOrganisation(OrganisationModel organisation);
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
                    organisation_type as OrganisationType
                    FROM organisations
                    WHERE id = @id",
                new { id = id });
                return organisation;
            }
        }

        public async Task<OrganisationModel> CreateOrganisation(OrganisationModelCreate organisation, int userId)
        {
            OrganisationModel result = new();
            DateTime now = DateTime.Now;
            result.CreatedAt = now;
            result.Name = organisation.Name;
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var newOrganistionId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO organisations (name, created_at, organisation_type)
                    VALUES (@name, @created_at, @organisation_type) RETURNING id",
                    new { 
                        name = organisation.Name, 
                        created_at = now, 
                        organisation_type = organisation.OrganisationType,
                    });
               
                result.Id = newOrganistionId;
            }
            
            return result;
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
    }
}
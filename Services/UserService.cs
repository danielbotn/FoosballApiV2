using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FoosballApi.Enums;
using FoosballApi.Models;
using FoosballApi.Models.DoubleLeagueMatches;
using FoosballApi.Models.Matches;
using FoosballApi.Models.Users;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsers(int currentOrganisationId);
        Task<User> GetUserById(int id);
        User GetUserByIdSync(int id);
        void UpdateUser(User user);
        void DeleteUser(User user);
    }

    public class UserService : IUserService
    {
        public string _connectionString { get; }

        public UserService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

       public async Task<List<User>> GetAllUsers(int currentOrganisationId)
       {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var users = await conn.QueryAsync<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE current_organisation_id = @currentOrganisationId",
                new { currentOrganisationId });
                return users.ToList();
            }
        }

        public async Task<User> GetUserById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstOrDefaultAsync<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE id = @id",
                    new { id });
                return user;
            }
        }

        public User GetUserByIdSync(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = conn.QueryFirstOrDefault<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, created_at, 
                    current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE id = @id",
                    new { id });
                return user;
            }
        }

        public void UpdateUser(User user)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE Users SET email = @email, first_name = @firstName, last_name = @lastName, 
                    current_organisation_id = @currentOrganisationId, photo_url = @photoUrl 
                    WHERE id = @id",
                    new { user.Email, user.FirstName, user.LastName, user.CurrentOrganisationId, user.PhotoUrl, user.Id });
            }
        }

        public void DeleteUser(User user)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    "DELETE FROM Users WHERE id = @id",
                    new { user.Id });
            }
        }
    }
}
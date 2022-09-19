using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using FoosballApi.Helpers;
using FoosballApi.Models;
using FoosballApi.Models.Accounts;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace FoosballApi.Services
{
    public interface IAuthService
    {
        Task<User> Authenticate(string username, string password);
        void CreateUser(User user);
        // bool VerifyEmail(string token);
        Task<bool> VerifyCode(string token, int userId);
        // VerificationModel ForgotPassword(ForgotPasswordRequest model, string origin);
        // bool SaveChanges();
        VerificationModel AddVerificationInfo(User user, string origin);
        // void ResetPassword(ResetPasswordRequest model);
        string CreateToken(User user);
    }

    public class AuthService : IAuthService
    {
        public string _connectionString { get; }

        public AuthService()
        {
            #if DEBUG
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbDev");
            #else
                _connectionString = Environment.GetEnvironmentVariable("FoosballDbProd");
            #endif
        }

        public async Task<User> Authenticate(string email, string password)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var user = await conn.QueryFirstOrDefaultAsync<User>(
                    @"SELECT id, email, password as Password, first_name as FirstName, last_name as LastName, 
                    created_at, current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE email = @email",
                    new { email });

                if (user == null)
                    return null;
                
                bool verified = BCrypt.Net.BCrypt.Verify(password, user.Password);

                if (!verified)
                    return null;
                
                return user;
            }
        }

        public void CreateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            // get random number
            Random rnd = new Random();
            int randomNumber = rnd.Next(1, 99999);
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(user.Password);
            DateTime now = DateTime.Now;
            User tmpUser = new User();
            tmpUser.Email = user.Email;
            tmpUser.Password = passwordHash;
            tmpUser.FirstName = user.FirstName;
            tmpUser.LastName = user.LastName;
            tmpUser.Created_at = now;
            tmpUser.PhotoUrl = "https://avatars.dicebear.com/api/personas/:" + randomNumber + ".png";
            
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"INSERT INTO Users (email, password, first_name, last_name, created_at, photo_url)
                    VALUES (@email, @password, @first_name, @last_name, @created_at, @photo_url)",
                    new {
                        email = tmpUser.Email, 
                        password = tmpUser.Password, 
                        first_name = tmpUser.FirstName, 
                        last_name = tmpUser.LastName, 
                        created_at = tmpUser.Created_at, 
                        photo_url = tmpUser.PhotoUrl
                    });
            }
        }

        // public VerificationModel ForgotPassword(ForgotPasswordRequest model, string origin)
        // {
        //     var account = _context.Users.SingleOrDefault(x => x.Email == model.Email);

        //     VerificationModel vModel = _context.Verifications.SingleOrDefault(x => x.UserId == account.Id);

        //     // always return ok response to prevent email enumeration
        //     if (account == null) return null;

        //     // create reset token that expires after 1 day
        //     vModel.PasswordResetToken = RandomTokenString();
        //     vModel.PasswordResetTokenExpires = DateTime.UtcNow.AddDays(1);

        //     _context.Verifications.Update(vModel);
        //     _context.SaveChanges();

        //     return vModel;
        // }

        // public bool SaveChanges()
        // {
        //     return (_context.SaveChanges() >= 0);
        // }

        private async Task<VerificationModel> GetVerificationModel(int userId, string token)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                return await conn.QueryFirstOrDefaultAsync<VerificationModel>(
                    @"SELECT id, user_id as UserId, verification_token as VerificationToken, 
                    password_reset_token as PasswordResetToken, has_verified as HasVerified
                    FROM Verifications WHERE user_id = @userId AND verification_token = @token",
                    new { userId, token });
            }

        }

        private void UpdateVerificationTable(VerificationModel vModel)
        {
            string verificationToken = null;
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"UPDATE Verifications SET has_verified = @hasVerified, verification_token = @verificationToken  WHERE id = @id",
                    new { hasVerified = true, verificationToken = verificationToken, id = vModel.Id });
            }
        }

        public async Task<bool> VerifyCode(string token, int userId)
        {
            var vModel = await GetVerificationModel(userId, token);
            if (vModel == null) return false;
            if (vModel.VerificationToken == token)
            {
                UpdateVerificationTable(vModel);
                return true;
            }
            return false;
        }

        // public bool VerifyEmail(string token)
        // {
        //     bool hasVerified = false;
        //     var account = _context.Verifications.SingleOrDefault(x => x.VerificationToken == token);

        //     if (account == null)
        //     {
        //         throw new ArgumentNullException(nameof(account));
        //     }

        //     account.HasVerified = true;
        //     account.VerificationToken = null;

        //     _context.Verifications.Update(account);
        //     _context.SaveChanges();

        //     if (account != null) 
        //     {
        //         hasVerified = true;
        //     }
        //     else
        //     {
        //         hasVerified = false;
        //     }
        //     return hasVerified;
        // }

        public VerificationModel AddVerificationInfo(User user, string origin)
        {
            VerificationModel vModel = new VerificationModel();
            vModel.UserId = user.Id;
            vModel.VerificationToken = RandomTokenString();
            vModel.HasVerified = false;
            
            // using dapper
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"INSERT INTO Verifications (user_id, verification_token, has_verified)
                    VALUES (@user_id, @verification_token, @has_verified)",
                    new {user_id = user.Id, verification_token = vModel.VerificationToken, has_verified = vModel.HasVerified});
            }

            return vModel;
        }

        private string RandomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            string token = BitConverter.ToString(randomBytes).Replace("-", "");
            string firstFiveOfToken = token.Substring(0, 5);
            return firstFiveOfToken;
        }

        // public void ResetPassword(ResetPasswordRequest model)
        // {
        //     VerificationModel vModel = _context.Verifications.SingleOrDefault(x =>
        //         x.PasswordResetToken == model.Token &&
        //         x.PasswordResetTokenExpires > DateTime.UtcNow);

        //     if (vModel == null)
        //         throw new AppException("Invalid token");

        //     User user = _context.Users.SingleOrDefault(x => x.Id == vModel.UserId);

        //     string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

        //     // update password and remove reset token
        //     user.Password = passwordHash;
        //     vModel.PasswordResetTokenExpires = DateTime.UtcNow;
        //     vModel.PasswordResetToken = null;

        //     _context.Verifications.Update(vModel);
        //     _context.Users.Update(user);
        //     _context.SaveChanges();
        // }

        public string CreateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = Environment.GetEnvironmentVariable("JWTSecret");
            var key = Encoding.ASCII.GetBytes(jwt);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString()),
                    new Claim("name", user.Id.ToString()),
                    new Claim("CurrentOrganisationId", user.CurrentOrganisationId.ToString()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return tokenString;
        }

    }
}
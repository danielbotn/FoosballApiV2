using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using FoosballApi.Models;
using FoosballApi.Models.Accounts;
using FoosballApi.Models.OldRefreshTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using FoosballApi.Dtos.Users;
using FoosballApi.Models.Google;
using System.Text.Json;

namespace FoosballApi.Services
{
    public interface IAuthService
    {
        Task<User> Authenticate(string username, string password);
        void CreateUser(User user);
        // bool VerifyEmail(string token);
        Task<bool> VerifyCode(string token, int userId);
        Task<VerificationModel> ForgotPassword(ForgotPasswordRequest model, string origin);
        Task<UpdatePasswordModel> UpdatePassword(UpdatePasswordRequest model, int userId);
        // bool SaveChanges();
        VerificationModel AddVerificationInfo(User user, string origin, bool? hasVerified = false);
        // void ResetPassword(ResetPasswordRequest model);
        string CreateToken(User user);
        string GenerateRefreshToken();
        Task<bool> SaveRefreshTokenToDatabase(string refreshToken, int userId);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<(bool, int)> IsOldRefreshTokenInDatabase(User user, string refreshToken);
        Task DeleteOldRefreshTokenById(int id);
        Task DeleteOldTokens(int? organisationId);
        Task<User> RegisterGoogleUser(GoogleUserDto dto);
        Task<GoogleUserInfo> GetGoogleUserInfoFromAccessToken(string accessToken);
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
            tmpUser.PhotoUrl = "https://avatars.dicebear.com/7.x/personas/png?seed=" + randomNumber;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"INSERT INTO Users (email, password, first_name, last_name, created_at, photo_url, provider)
                    VALUES (@email, @password, @first_name, @last_name, @created_at, @photo_url, @provider)",
                    new
                    {
                        email = tmpUser.Email,
                        password = tmpUser.Password,
                        first_name = tmpUser.FirstName,
                        last_name = tmpUser.LastName,
                        created_at = tmpUser.Created_at,
                        photo_url = tmpUser.PhotoUrl,
                        provider = "LOCAL"
                    });
            }
        }

        private async Task<User> GetUserEmail(string email)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                    @"SELECT id, email, first_name as FirstName, last_name as LastName, 
                    created_at, current_organisation_id as CurrentOrganisationId, photo_url as PhotoUrl 
                    FROM Users WHERE email = @email",
                    new { email });

            return user;
        }

        private async Task<VerificationModel> GetVerificationInfo(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<VerificationModel>(
                    @"SELECT id as Id, user_id as UserId, verification_token as VerificationToken, password_reset_token as PasswordResetToken,
                    password_reset_token_expires as PasswordResetTokenExpires, has_verified as HasVerified
                    FROM verifications WHERE user_id = @user_id",
                    new { user_id = userId });

            return user;
        }

        private async Task UpdateVerification(VerificationModel vModel)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(
               @"UPDATE verifications 
                SET password_reset_token = @password_reset_token,
                password_reset_token_expires = @password_reset_token_expires::timestamp without time zone
                WHERE id = @id",
               new
               {
                   password_reset_token = vModel.PasswordResetToken,
                   password_reset_token_expires = vModel.PasswordResetTokenExpires,
                   id = vModel.Id
               });
        }

        public async Task<VerificationModel> ForgotPassword(ForgotPasswordRequest model, string origin)
        {
            var account = await GetUserEmail(model.Email);
            // always return ok response to prevent email enumeration
            if (account == null) return null;

            VerificationModel vModel = await GetVerificationInfo(account.Id);

            // create reset token that expires after 1 day
            vModel.PasswordResetToken = RandomTokenString();
            vModel.PasswordResetTokenExpires = DateTime.UtcNow.AddDays(1);

            await UpdateVerification(vModel);

            return vModel;
        }

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

        private async Task<VerificationModel> GetVerificationModelById(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<VerificationModel>(
                @"SELECT id, user_id as UserId, verification_token as VerificationToken, 
                    password_reset_token as PasswordResetToken, has_verified as HasVerified,
                    change_password_token as ChangePasswordToken, change_password_token_expires
                    AS ChangePasswordTokenExpires, change_password_verification_token as ChangePasswordVerificationToken
                    FROM Verifications WHERE user_id = @userId",
                new { userId });

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

        public VerificationModel AddVerificationInfo(User user, string origin, bool? hasVerified = false)
        {
            VerificationModel vModel = new VerificationModel();
            vModel.UserId = user.Id;
            vModel.VerificationToken = GetRandomTokenString();
            vModel.HasVerified = hasVerified ?? false;

            // using dapper
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Execute(
                    @"INSERT INTO Verifications (user_id, verification_token, has_verified)
                    VALUES (@user_id, @verification_token, @has_verified)",
                    new { user_id = user.Id, verification_token = vModel.VerificationToken, has_verified = vModel.HasVerified });
            }

            return vModel;
        }


        public async Task<User> RegisterGoogleUser(GoogleUserDto dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            const string selectSql = "SELECT * FROM users WHERE email = @Email";
            var existingUser = await conn.QueryFirstOrDefaultAsync<User>(selectSql, new { dto.Email });
            
            if (existingUser != null)
                return existingUser;

            var newUser = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhotoUrl = dto.PictureUrl,
                GoogleId = dto.GoogleId,
                AuthProvider = "Google",
                Created_at = DateTime.UtcNow,
                Password = "Google-Login-User"
            };

            const string insertSql = @"
                INSERT INTO users (email, first_name, last_name, photo_url, google_id, auth_provider, created_at, password)
                VALUES (@Email, @FirstName, @LastName, @PhotoUrl, @GoogleId, @AuthProvider, @Created_at, @Password)
                RETURNING id";

            var insertedId = await conn.QuerySingleAsync<int>(insertSql, newUser);
            newUser.Id = insertedId;

            return newUser;
        }

        private string GetRandomTokenString()
        {
            var randomNumber = new byte[40]; // or 32
            string token = "";

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                token = Convert.ToBase64String(randomNumber);
            }
            string firstFiveOfToken = token.Substring(0, 5);
            return firstFiveOfToken;
        }

        private static string RandomTokenString()
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
            var jwt = Environment.GetEnvironmentVariable("JwtSecret");
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

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private async Task SaveOldRefreshTokenToDatabase(string refreshToken, DateTime expirationTime, User user)
        {
            using var conn = new NpgsqlConnection(_connectionString);

            if (user.RefreshToken != null)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO old_refresh_tokens (refresh_token, refresh_token_expiry_time, fk_user_id, fk_organisation_id, inserted_at)
                                VALUES (@refresh_token, @refresh_token_expiry_time, @fk_user_id, @fk_organisation_id, @inserted_at)",
                            new
                            {
                                refresh_token = user.RefreshToken,
                                refresh_token_expiry_time = user.RefreshTokenExpiryTime,
                                fk_user_id = user.Id,
                                fk_organisation_id = user.CurrentOrganisationId,
                                inserted_at = DateTime.Now
                            });
            }
        }


        private async Task<User> GetUserById(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                @"SELECT u.id, u.email, u.first_name as FirstName, u.last_name as LastName, u.created_at, 
                    u.current_organisation_id as CurrentOrganisationId, u.photo_url as PhotoUrl , o.is_admin as IsAdmin,
                    u.refresh_token as RefreshToken, u.refresh_token_expiry_time as RefreshTokenExpiryTime,
                    o.is_deleted as IsDeleted
                    FROM Users u
                    JOIN organisation_list o ON o.user_id = u.id AND o.organisation_id = u.current_organisation_id
                    WHERE u.id = @id",
                new { id });
            return user;
        }

        private async Task<User> GetUserWithoutJoin(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            var user = await conn.QueryFirstOrDefaultAsync<User>(
                @"SELECT u.id, u.email, u.first_name as FirstName, u.last_name as LastName, u.created_at, 
                    u.current_organisation_id as CurrentOrganisationId, u.photo_url as PhotoUrl,
                    u.refresh_token as RefreshToken, u.refresh_token_expiry_time as RefreshTokenExpiryTime
                    FROM Users u
                    WHERE u.id = @id",
                new { id });
            return user;
        }

        public async Task<bool> SaveRefreshTokenToDatabase(string refreshToken, int userId)
        {
            bool result = false;
            var user = await GetUserWithoutJoin(userId);
            await SaveOldRefreshTokenToDatabase(user.RefreshToken, user.RefreshTokenExpiryTime, user);
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int updateSuccessfull = await conn.ExecuteAsync(
                    @"UPDATE users 
                    SET refresh_token = @refresh_token, 
                    refresh_token_expiry_time = @refresh_token_expiry_time
                    WHERE id = @id",
                    new
                    {
                        refresh_token = refreshToken,
                        refresh_token_expiry_time = DateTime.Now.AddDays(7),
                        id = userId
                    });

                if (updateSuccessfull > 0)
                    result = true;
            }

            return result;
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWTSecret"));
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = false,//here we are saying that we don't care about the token's expiration date
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken securityToken;
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");
            return principal;
        }

        public async Task<(bool, int)> IsOldRefreshTokenInDatabase(User user, string refreshToken)
        {
            bool result = false;
            int id = 0;

            using (var conn = new NpgsqlConnection(_connectionString))
            {
                var oldRefreshTokens = await conn.QueryFirstOrDefaultAsync<OldRefreshToken>(
                    @"SELECT id, refresh_token AS RefreshToken, refresh_token_expiry_time as RefreshTokenExpiryTime,
                    fk_user_id AS UserId, fk_organisation_id AS OrganisationId
                    FROM old_refresh_tokens
                    WHERE fk_user_id = @fk_user_id AND fk_organisation_id = @fk_organisation_id AND refresh_token = @refresh_token",
                    new { fk_user_id = user.Id, fk_organisation_id = user.CurrentOrganisationId, refresh_token = refreshToken });

                if (oldRefreshTokens != null && oldRefreshTokens.RefreshToken == refreshToken && oldRefreshTokens.RefreshTokenExpiryTime <= DateTime.Now)
                {
                    result = true;
                    id = oldRefreshTokens.Id;
                }
            }

            return (result, id);
        }


        public async Task DeleteOldRefreshTokenById(int id)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    @"DELETE FROM old_refresh_tokens WHERE id = @id",
                    new { id = id });
            }
        }

        public async Task DeleteOldTokens(int? organisationId)
        {
            using var conn = new NpgsqlConnection(_connectionString);

            if (organisationId is not null)
            {
                var expiryTime = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(); // calculate the expiry time as 24 hours ago in UTC time
                await conn.ExecuteAsync(
                    @"DELETE FROM old_refresh_tokens 
                        WHERE fk_organisation_id = @fk_organisation_id AND refresh_token_expiry_time < to_timestamp(@expiry_time)",
                    new { fk_organisation_id = organisationId, expiry_time = expiryTime });
            }
        }

        private async Task<bool> UpdateVerificationChangePasswordFields(int userId)
        {
            bool result = true;
            using var conn = new NpgsqlConnection(_connectionString);

            var data = await conn.ExecuteAsync(
                @"UPDATE verifications 
                SET 
                    change_password_verification_token = @change_password_verification_token,
                    change_password_token_expires = @change_password_token_expires::timestamp without time zone,
                    change_password_token = @change_password_token
                WHERE 
                    user_id = @user_id",
                new
                {
                    change_password_verification_token = GetRandomTokenString(),
                    change_password_token_expires = DateTime.UtcNow.AddDays(1),
                    change_password_token = RandomTokenString(),
                    user_id = userId
                });

            if (data == 0)
                result = false;

            return result;
        }

        public async Task<bool> UpdateUserPassword(string password, int userId)
        {
            bool result = false;
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                int updateSuccessfull = await conn.ExecuteAsync(
                    @"UPDATE users 
                    SET password = @password
                    WHERE id = @id",
                    new
                    {
                        password = passwordHash,
                        id = userId
                    });

                if (updateSuccessfull > 0)
                    result = true;
            }

            return result;
        }

        private async Task CleanVerificationAfterUpdate(int userId)
        {
            using var conn = new NpgsqlConnection(_connectionString);

            // Define nullable values
            DateTime? changePasswordTokenExpires = null;
            string changePasswordToken = null;
            string changePasswordVerificationToken = null;

            int updateSuccessful = await conn.ExecuteAsync(
                @"UPDATE verifications 
                    SET change_password_token_expires = @change_password_token_expires,
                        change_password_token = @change_password_token,
                        change_password_verification_token = @change_password_verification_token
                    WHERE user_id = @user_id",
                new
                {
                    change_password_token_expires = changePasswordTokenExpires,
                    change_password_token = changePasswordToken,
                    change_password_verification_token = changePasswordVerificationToken,
                    user_id = userId
                });
        }



        public async Task<UpdatePasswordModel> UpdatePassword(UpdatePasswordRequest model, int userId)
        {
            UpdatePasswordModel result = new();
            if (model.VerificationCode == null)
            {
                // send email with verification code
                // email is sent later in the controller
                bool databaseUpdated = await UpdateVerificationChangePasswordFields(userId);

                if (databaseUpdated)
                {
                    var verification = await GetVerificationModelById(userId);
                    result.VerificationModel = verification;
                    result.VerificationCodeCreated = true;
                }
            }
            else
            {
                // chech code and update password
                var verification = await GetVerificationModelById(userId);

                if (model.VerificationCode == verification.ChangePasswordVerificationToken && model.Password == model.ConfirmPassword)
                {
                    // update database 
                    await UpdateUserPassword(model.Password, userId);
                    result.VerificationModel = verification;
                    result.PasswordUpdated = true;
                    await CleanVerificationAfterUpdate(userId);
                }
            }
            return result;
        }

        public async Task<GoogleUserInfo> GetGoogleUserInfoFromAccessToken(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
                
                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return userInfo;
            }
            catch
            {
                return null;
            }
        }
    }
}

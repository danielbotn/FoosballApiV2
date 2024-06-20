using System;
using AutoMapper;
using FoosballApi.Dtos.Users;
using FoosballApi.Models;
using FoosballApi.Models.Accounts;
using FoosballApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoosballApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public AuthController(IAuthService authService, IUserService userService, IMapper mapper, IEmailService emailService)
        {
            _authService = authService;
            _userService = userService;
            _mapper = mapper;
            _emailService = emailService;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(UserLogin), StatusCodes.Status200OK)]
        public async Task<ActionResult> Authenticate([FromBody] AuthenticateModel model)
        {
            try
            {
                var user = await _authService.Authenticate(model.Username, model.Password);

                if (user == null)
                    return StatusCode(401, "Username or password is incorrect");

                string tokenString = _authService.CreateToken(user);
                string refreshToken = _authService.GenerateRefreshToken();
                bool refreshTokenToDatabaseSuccessfull = await _authService.SaveRefreshTokenToDatabase(refreshToken, user.Id);

                if (!refreshTokenToDatabaseSuccessfull)
                    return StatusCode(500, "Error, could not save refresh token to database");

                UserLogin userLogin = new UserLogin
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = tokenString,
                    RefreshToken = refreshToken
                };

                return Ok(userLogin);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(UserReadDto), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateUser(UserCreateDto userCreateDto)
        {
            try
            {
                var userModel = _mapper.Map<User>(userCreateDto);
                var user = _userService.GetUserByEmail(userCreateDto.Email);

                if (user != null)
                    return Conflict();

                _authService.CreateUser(userModel);
                var tmpUser = _userService.GetUserByEmail(userCreateDto.Email);
                var vModel = _authService.AddVerificationInfo(tmpUser, Request.Headers["origin"]);

                var userReadDto = _mapper.Map<UserReadDto>(tmpUser);

                await _emailService.SendVerificationEmail(vModel, tmpUser, Request.Headers["origin"]);

                return CreatedAtRoute(nameof(UsersController.GetUserById), new { Id = userReadDto.Id }, userReadDto);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(UserVerify), StatusCodes.Status200OK)]
        public async Task<ActionResult> VerifyEmail(VerifyEmailRequest model)
        {
            try
            {
                bool isCodeValid = await _authService.VerifyCode(model.Token, model.UserId);

                if (isCodeValid)
                {
                    UserVerify userVerify = new UserVerify
                    {
                        Message = "Verification successful, you can now login"
                    };
                    return Ok(userVerify);
                }
                else
                {
                    // return statusCode not allowed with a message
                    return StatusCode(StatusCodes.Status403Forbidden, "Verification code is invalid");
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        // used for mobile refresh only
        [HttpPost]
        [Route("refresh")]
        [ProducesResponseType(typeof(UserLogin), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Refresh([FromBody] TokenApiModel tokenApiModel)
        {
            try
            {
                if (tokenApiModel is null)
                    return BadRequest("Invalid client request");

                string accessToken = tokenApiModel.Token;
                string refreshToken = tokenApiModel.RefreshToken;
                var principal = _authService.GetPrincipalFromExpiredToken(accessToken);
                var username = principal.Identity.Name;
                
                var name = principal.FindFirst("name").Value;
                var userId = int.Parse(name);
                var user = await _userService.GetUserById(userId);
                if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
                {
                    if (user.RefreshToken != refreshToken)
                    {
                        // check if old refresh token is in database
                        var isRTokenInDatabase = await _authService.IsOldRefreshTokenInDatabase(user, refreshToken);
                        if (isRTokenInDatabase.Item1 == false)
                        {
                            return BadRequest("Invalid client request from refresh endpoint");
                        }
                    }
                    
                }

                if (user.CurrentOrganisationId is not null)
                {
                    await _authService.DeleteOldTokens((int)user.CurrentOrganisationId);
                }

                string tokenString = _authService.CreateToken(user);
                string newRefreshToken = _authService.GenerateRefreshToken();
                bool refreshTokenToDatabaseSuccessfull = await _authService.SaveRefreshTokenToDatabase(newRefreshToken, userId);

                if (!refreshTokenToDatabaseSuccessfull)
                    return StatusCode(500, "Error, could not save refresh token to database");
                
                return Ok(new UserLogin()
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = tokenString,
                    RefreshToken = newRefreshToken
                });
            }
            catch(Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(UserForgotPassword), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest model)
        {
            try
            {
                var verification = await _authService.ForgotPassword(model, Request.Headers["origin"]);
                var user = _userService.GetUserByEmail(model.Email);
                await _emailService.SendPasswordResetEmail(verification, user, Request.Headers["origin"], model);
                UserForgotPassword userForgotPassword = new()
                {
                    Message = "Reset email was successfully sent to user"
                };
                return Ok(userForgotPassword);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPut("update-password")]
        [ProducesResponseType(typeof(UpdatePasswordResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordRequest model)
        {
            try
            {
                string userId = User.Identity.Name;
                var verification = await _authService.UpdatePassword(model, int.Parse(userId));
                var user = await _userService.GetUserById(int.Parse(userId));
                
                bool emailSent = false;

                if (model.VerificationCode == null && verification.VerificationCodeCreated)
                {
                    if (verification.VerificationModel.ChangePasswordTokenExpires < DateTime.UtcNow)
                    {
                        // Token is expired, return Bad Request
                        return StatusCode(StatusCodes.Status400BadRequest, "Verification token has expired.");
                    }

                    await _emailService.SendVerificationChangePasswordEmail(verification.VerificationModel, user);
                    emailSent = true;
                }
                
                var response = new UpdatePasswordResponse
                {
                    EmailSent = emailSent,
                    VerificationCodeCreated = verification.VerificationCodeCreated,
                    PasswordUpdated = verification.PasswordUpdated
                };

                return Ok(response);
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }



        // [HttpPost("reset-password")]
        // [ProducesResponseType(typeof(UserForgotPassword), StatusCodes.Status200OK)]
        // public IActionResult ResetPassword(ResetPasswordRequest model)
        // {
        //     try
        //     {
        //         _authService.ResetPassword(model);
        //         UserForgotPassword userResetPassword = new UserForgotPassword
        //         {
        //             Message = "Password reset successful, you can now login"
        //         };
        //         return Ok(userResetPassword);
        //     }
        //     catch (Exception e)
        //     {
        //         return StatusCode(500, e.Message);
        //     }
        // }
    }
}
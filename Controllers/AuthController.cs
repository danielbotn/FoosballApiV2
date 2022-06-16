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
                    return BadRequest(new { message = "Username or password is incorrect" });

                string tokenString = _authService.CreateToken(user);

                UserLogin userLogin = new UserLogin
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Token = tokenString
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
        public ActionResult<UserReadDto> CreateUser(UserCreateDto userCreateDto)
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

                _emailService.SendVerificationEmail(vModel, tmpUser, Request.Headers["origin"]);

                return CreatedAtRoute(nameof(UsersController.GetUserById), new { Id = userReadDto.Id }, userReadDto);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        // [HttpPost("verify-email")]
        // [ProducesResponseType(typeof(UserVerify), StatusCodes.Status200OK)]
        // public IActionResult VerifyEmail(VerifyEmailRequest model)
        // {
        //     try
        //     {
        //         bool isCodeValid = _authService.VerifyCode(model.Token, model.UserId);

        //         if (isCodeValid)
        //         {
        //             UserVerify userVerify = new UserVerify
        //             {
        //                 Message = "Verification successful, you can now login"
        //             };
        //             return Ok(userVerify);
        //         }
        //         else
        //         {
        //             // return statusCode not allowed with a message
        //             return StatusCode(StatusCodes.Status403Forbidden, "Verification code is invalid");
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         return StatusCode(500, e.Message);
        //     }
        // }

        // [HttpPost("forgot-password")]
        // [ProducesResponseType(typeof(UserForgotPassword), StatusCodes.Status200OK)]
        // public IActionResult ForgotPassword(ForgotPasswordRequest model)
        // {
        //     try
        //     {
        //         var verification = _authService.ForgotPassword(model, Request.Headers["origin"]);
        //         var user = _userService.GetUserByEmail(model.Email);
        //         _emailService.SendPasswordResetEmail(verification, user, Request.Headers["origin"]);
        //         UserForgotPassword userForgotPassword = new UserForgotPassword
        //         {
        //             Message = "Password reset successful, you can now login"
        //         };
        //         return Ok(userForgotPassword);
        //     }
        //     catch (Exception e)
        //     {
        //         return StatusCode(500, e.Message);
        //     }
        // }

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
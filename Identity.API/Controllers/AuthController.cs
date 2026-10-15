using Identity.API.Application.Interfaces;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserService userService,
    IAuthenticationService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var result = await userService.AddUserAsync(request.UserName, request.FirstName, request.LastName, request.Email, request.Password);

        if (result.IsSuccess)
        {
            return Ok(new { Token = result, Message = "User succesfully registired." });
        }

        return BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await authService.LoginAsync(request.Email, request.Password);

        if (token == null)
        {
            return Unauthorized("Email or password is incorrect.");
        }

        return Ok(new { Token = token });
    }

    [HttpGet]
    public async Task<IActionResult> GetUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("Invalid or broken token.");
        }

        var userDto = await userService.GetUserByIdAsync(Guid.Parse(userId));

        if (userDto == null)
        {
            return NotFound(new { Error = "Could not found user." });
        }

        return Ok(userDto);
    }
}
using Identity.API.Application.Interfaces;
using Identity.API.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, IAuthenticationService authService) : ControllerBase
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
            return Unauthorized("Email or password is incorrect.");

        return Ok(new { Token = token });
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        var userDto = await userService.GetUserByIdAsync(userId);

        if (userDto == null)
            return NotFound(new { Error = "Could not found user." });

        return Ok(userDto);
    }
}
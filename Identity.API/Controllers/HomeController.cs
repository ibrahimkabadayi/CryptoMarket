using Identity.API.Application.Interfaces;
using Identity.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

public class HomeController(IUserService userService) : Controller
{
    private readonly IUserService _userService = userService;

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUser(Guid userId)
    {
        //var userDto = await _userService.GetUserByIdAsync(userId);

        throw new NotImplementedException();
    }

    [HttpPost("~/Home/RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
    {
        var result = await _userService.AddUserAsync(request.UserName, request.Email, request.Password);

        if(result == "User Added")
        {
            return Ok();
        } 
        else if (result == "This user is already registired.")
        {
            return BadRequest(result);
        }
            
        return BadRequest(result);
    }
}

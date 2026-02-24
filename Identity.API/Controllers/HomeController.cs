using Identity.API.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    public class HomeController(IUserService userService) : Controller
    {
        private readonly IUserService _userService = userService;

        [HttpGet("{userId:guid}")]
        public IActionResult GetUser(Guid userId)
        {
            //var userDto = _userService.GetUserByIdAsync(userId);

            throw new NotImplementedException();
        }

    }
}

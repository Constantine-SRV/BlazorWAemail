using BlazorWAemail.Serve.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorWAemail.Serve.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRolesService _rolesService;

        public UserRolesController(IUserRolesService rolesService)
        {
            _rolesService = rolesService;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetUserRoles()
        {
            // Получаем email пользователя из токена (claim "email" или Name)
            var email = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email))
                return Forbid();

            var roles = await _rolesService.GetUserRolesByEmailAsync(email);

            if (roles == null || roles.Count == 0)
                return NotFound();

            return Ok(roles);
        }
    }
}
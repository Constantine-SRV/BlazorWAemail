using BlazorWAemail.Server.Models;      // ваш DbContext и сущности
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWAemail.Serve.Services
{
    public interface IUserRolesService
    {
        Task<List<string>> GetUserRolesByEmailAsync(string email);
    }

    public class UserRolesService : IUserRolesService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserRolesService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<string>> GetUserRolesByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return new() { "Guest" };

            // Users → UserRoles → Role  (точно так же, как в старом проекте)
            var user = await _dbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Email == email);

            if (user == null || user.UserRoles == null || user.UserRoles.Count == 0)
                return new() { "Guest" };

            return user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        }
    }
}

using BlazorWAemail.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWAemail.Server.Services
{
    public class AppSettingsService
    {
        private readonly ApplicationDbContext _dbContext;

        public AppSettingsService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Dictionary<string, string>> GetAppSettingsAsync()
        {
            var settings = await _dbContext.AppSettings.ToListAsync();
            var settingsDict = new Dictionary<string, string>();

            foreach (var setting in settings)
            {
                settingsDict[setting.Key] = setting.Value;
            }

            return settingsDict;
        }
    }
}

using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BlazorWAemail.Client.Services
{
    public class GetUserRolesService
    {
        private readonly HttpClient _httpClient;

        public GetUserRolesService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<string>> GetUserRoles()
        {
            return await _httpClient.GetFromJsonAsync<List<string>>("api/UserRoles") ?? new List<string>();
        }
    }
}

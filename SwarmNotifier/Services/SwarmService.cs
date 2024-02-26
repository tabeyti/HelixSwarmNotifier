using Serilog;
using SwarmNotifier.Configurations;
using SwarmNotifier.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SwarmNotifier.Services
{
    public class SwarmService
    {
        /// <summary>
        /// Currently cached groups.
        /// </summary>
        public List<SwarmGroup>? Groups => _groups;

        private readonly SwarmConfiguration _config;
        private readonly HttpClient _client;
        private List<SwarmGroup>? _groups = new();

        public readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public SwarmService(SwarmConfiguration config)
        {
            _config = config;
            _client = new();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_config.Username}:{_config.Token}")));

            _ = Initialize();
        }

        public async Task Initialize()
        {
            _groups = await GetGroups();
            if (null == _groups)
            {
                Log.Logger.Error("Could not retrieve Swarm groups.");
            }
        }

        public async Task<SwarmReview?> GetReview(string id)
        {
            var response = await _client.GetAsync($"{_config.ApiUri}/reviews/{id}");
            response.EnsureSuccessStatusCode();

            string jsonText = await response.Content.ReadAsStringAsync();
            SwarmResponse<SwarmReviewsData>? swarmResponse = JsonSerializer.Deserialize<SwarmResponse<SwarmReviewsData>>(jsonText, _jsonOptions);
            if (null == swarmResponse?.Data?.Reviews) { return null; }
            return swarmResponse.Data.Reviews.First();
        }

        public async Task<List<SwarmReview>?> GetReviews()
        {
            var response = await _client.GetAsync($"{_config.ApiUri}/reviews");
            response.EnsureSuccessStatusCode();
            
            string jsonText = await response.Content.ReadAsStringAsync();
            SwarmResponse<SwarmReviewsData>? swarmResponse = JsonSerializer.Deserialize<SwarmResponse<SwarmReviewsData>>(jsonText, _jsonOptions);
            if (null == swarmResponse?.Data?.Reviews) { return null; }
            return swarmResponse.Data.Reviews;
        }

        public async Task<List<SwarmGroup>?> GetGroups()
        {
            var response = await _client.GetAsync($"{_config.ApiUri}/groups");
            response.EnsureSuccessStatusCode();

            string jsonText = await response.Content.ReadAsStringAsync();
            SwarmResponse<SwarmGroupsData>? swarmResponse = JsonSerializer.Deserialize<SwarmResponse<SwarmGroupsData>>(jsonText, _jsonOptions);
            if (null == swarmResponse?.Data?.Groups) { return null; }
            return swarmResponse.Data.Groups;
        }

        public async Task<List<SwarmTestRun>?> GetReviewLatestTestRuns(int reviewId)
        {
            var response = await _client.GetAsync($"{_config.ApiUri}/reviews/{reviewId}/testruns");
            response.EnsureSuccessStatusCode();

            string jsonText = await response.Content.ReadAsStringAsync();
            SwarmResponse<SwarmTestRunData>? swarmResponse = JsonSerializer.Deserialize<SwarmResponse<SwarmTestRunData>>(jsonText, _jsonOptions);
            return swarmResponse?.Data?.TestRuns;
        }

        public async Task<SwarmUser?> GetUser(string username)
        {
            var response = await _client.GetAsync($"{_config.ApiUri}/users/{username}");
            response.EnsureSuccessStatusCode();

            string jsonText = await response.Content.ReadAsStringAsync();
            SwarmResponse<SwarmUsersData>? swarmResponse = JsonSerializer.Deserialize<SwarmResponse<SwarmUsersData>>(jsonText, _jsonOptions);
            return swarmResponse?.Data?.Users?.First();
        }
    }
}

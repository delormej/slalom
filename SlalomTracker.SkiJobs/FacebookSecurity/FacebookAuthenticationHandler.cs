using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace SlalomTracker.SkiJobs.FacebookSecurity
{
    public class FacebookAuthenticationHandler : IAuthenticationHandler
    {
        private AuthenticationScheme _scheme;
        private ClaimsPrincipal _principal;
        private HttpContext _context;       
        HttpClient _client;  
        static string _appAccessToken;
        string _appId;
        string _secret;
        string _authenticationError;
        string[] _adminUserIds;


        public FacebookAuthenticationHandler(IConfiguration configuration)
        {
            _client = new HttpClient();
            _appId = configuration.GetValue<string>("FacebookAppId");
            _secret = configuration.GetValue<string>("FacebookSecret");

            if (_appId == null || _secret == null)
                throw new ApplicationException("Did not find configuration values for FacebookAppId, FacebookSecret");

            _adminUserIds = ParseAdminUserIds(configuration);
        }

        private string[] ParseAdminUserIds(IConfiguration configuration)
        {
            string value = configuration.GetValue<string>("FacebookUserIds");
            if (string.IsNullOrEmpty(value))
                return null;
            else
                return value.Split(",");
        }

        public Task<AuthenticateResult> AuthenticateAsync()
        {
            if (_principal?.Identity.IsAuthenticated ?? false)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(_principal, _scheme.Name)));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail(_authenticationError));
            }
        }

        public Task ChallengeAsync(AuthenticationProperties properties)
        {
            _context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        public Task ForbidAsync(AuthenticationProperties properties)
        {
            _context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        public async Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _scheme = scheme;
            _context = context;

            if (_appAccessToken == null)
                _appAccessToken = await GetAppAccessTokenAsync();

            string accessToken = GetAccessToken();
            if (accessToken != null)
                _principal = await GetPrincipalAsync(accessToken);
        }

        private string GetAccessToken()
        {
            string header = _context.Request.Headers["Authorization"];
            if (header == null)
                return null;

            string[] values = header.Split();
            if (values.Length > 1 && !string.IsNullOrWhiteSpace(values[1]))
                return values[1];
            else
                return null;
        }

        private async Task<string> GetAppAccessTokenAsync()
        {
            string authUrl = $"https://graph.facebook.com/oauth/access_token?client_id={_appId}&client_secret={_secret}&grant_type=client_credentials";
            var response = await _client.GetStringAsync(authUrl);
            return GetTokenFromResponse(response);
        }

        private string GetTokenFromResponse(string response)
        {
            string accessToken= null;

            var doc = JsonDocument.Parse(response);
            JsonElement prop;
            if (doc.RootElement.TryGetProperty("access_token", out prop))
                accessToken = prop.GetString();
            
            return accessToken;
        }

        private async Task<FacebookPrincipal> GetPrincipalAsync(string accessToken)
        {
            string debugTokenUrl = $"https://graph.facebook.com/debug_token?input_token={accessToken}&access_token={_appAccessToken}";
            var response = await _client.GetStringAsync(debugTokenUrl);
            DebugToken token = JsonSerializer.Deserialize<DebugToken>(response);           
            _authenticationError = token?.data?.error?.message;

            return new FacebookPrincipal(_adminUserIds, token);
        }
    }
}
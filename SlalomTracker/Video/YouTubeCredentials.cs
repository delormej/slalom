using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.YouTube.v3;
using System.Text.Json;
using SlalomTracker.Cloud;

namespace SlalomTracker.Video
{
    public class YouTubeCredentials
    {
        const string ClientSecretId = "YouTubeClientSecret";
        const string TokenSecretId = "YouTubeTokenId";
        const string YouTubeUserEnvName = "YOUTUBE_USER";

        public static UserCredential Create()
        {
            string userName = Environment.GetEnvironmentVariable(YouTubeUserEnvName);

            if (userName == null)
                throw new ApplicationException($"{YouTubeUserEnvName} env variable must be set");

            TokenResponse token = GetSavedToken();
            ClientSecrets clientSecrets = GetClientSecrets();

            string[] scopes = new[] { YouTubeService.Scope.YoutubeUpload };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = scopes
            });

            UserCredential credential = new UserCredential(flow, userName, token);
            return credential;            
        }

        private static ClientSecrets GetClientSecrets()
        {
            string json = GetSecret(ClientSecretId);
            return JsonSerializer.Deserialize<ClientSecrets>(json);
        }

        private static TokenResponse GetSavedToken()
        {
            string json = GetSecret(TokenSecretId);
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }

        private static string GetSecret(string secretId)
        {
            string googleProjectId =
                Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
            
            if (googleProjectId == null)
                throw new ApplicationException("GOOGLE_PROJECT_ID env variable must be set.");

            SecretManager manager = new SecretManager();
            return manager.AccessSecret(googleProjectId, secretId);
        }
    }
}
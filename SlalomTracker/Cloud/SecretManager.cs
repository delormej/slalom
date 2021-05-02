using System;
using System.Text;
using System.Text.RegularExpressions;
using Google.Cloud.SecretManager.V1;
using Google.Api.Gax.ResourceNames;
using Google.Protobuf;
using Logger = jasondel.Tools.Logger;

namespace SlalomTracker.Cloud
{
    public class SecretManager
    {
        private SecretManagerServiceClient _client;
        private const string SecretVersionId = "latest";

        public SecretManager()
        {
            // Create the client.
            _client = SecretManagerServiceClient.Create();
        }

        public String AccessSecret(string projectId, string secretId)
        {
            

            if (!IsValidSecretId(secretId))
                return null;

            // Build the resource name.
            SecretVersionName secretVersionName = new SecretVersionName(projectId, secretId, SecretVersionId);

            try 
            {
                // Call the API.
                AccessSecretVersionResponse result = _client.AccessSecretVersion(secretVersionName);

                // Convert the payload to a string. Payloads are bytes by default.
                String payload = result.Payload.Data.ToStringUtf8();
                return payload;
            }
            catch (Grpc.Core.RpcException e)
            {
                // Supress NotFound exception and return null.
                if (e.StatusCode == Grpc.Core.StatusCode.NotFound || 
                        e.StatusCode == Grpc.Core.StatusCode.InvalidArgument)
                    return null;
                else
                    throw;
            }
        }

        public void UpdateLatestSecret(string projectId, string secretId, string secretValue)
        {
            // Build the resource name.
            SecretName secretName = new SecretName(projectId, secretId);
            // Convert the payload to bytes.
            SecretPayload payload = new SecretPayload
            {
                Data = ByteString.CopyFrom(secretValue, Encoding.UTF8),
            };

            // Delete the old version
            SecretVersionName secretVersionName = new SecretVersionName(projectId, secretId, SecretVersionId);
            _client.DestroySecretVersion(secretVersionName, default);

            // Create the new version
            SecretVersion version = _client.AddSecretVersion(secretName, payload);
        }

        public void CreateSecret(string projectId, string secretId, string secretValue)
        {
            // Build the parent project name.
            ProjectName projectName = new ProjectName(projectId);

            // Build the secret to create.
            Secret secret = new Secret
            {
                Replication = new Replication
                {
                    Automatic = new Replication.Types.Automatic(),
                },
            };

            Secret createdSecret = _client.CreateSecret(projectName, secretId, secret);

            // Build a payload.
            SecretPayload payload = new SecretPayload
            {
                Data = ByteString.CopyFrom(secretValue, Encoding.UTF8),
            };

            // Add a secret version.
            SecretVersion createdVersion = _client.AddSecretVersion(createdSecret.SecretName, payload);

            // Access the secret version.
            AccessSecretVersionResponse result = _client.AccessSecretVersion(createdVersion.SecretVersionName);
        }     

        /// <summary>
        /// Secret names can only contain English letters (A-Z), numbers (0-9), dashes (-), and underscores (_)
        /// </summary>
        private bool IsValidSecretId(string secretId)
        {
            const string secretPattern = @"^[a-zA-Z0-9_.-]+$";
            return Regex.IsMatch(secretId, secretPattern, RegexOptions.IgnoreCase);
        }
    }
}
